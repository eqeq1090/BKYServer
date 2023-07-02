using BKServerBase.Logger;
using BKServerBase.Util;
using Prometheus;
using System.Collections.Concurrent;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Interface;

namespace BKServerBase.Threading
{
    public class ThreadCoordinator
    {
        private static long m_runnableIDCounter;
        private static readonly Gauge s_gauge = Metrics.CreateGauge("threadcoordinator", "thread", "kind");

        private int m_MaxThreadWorkerCount;
        private int m_MaxThreadWorkerFramePerSecond;
        private ConcurrentDictionary<int, StandAloneThread> m_StandaloneThreads = new ConcurrentDictionary<int, StandAloneThread>();
        private int m_StandaloneThreadGenerator;
        private Dictionary<int, ThreadWorker> m_ThreadWorkers = new Dictionary<int, ThreadWorker>();
        private ConcurrentQueue<(long, IRunnable)> m_Runnables = new ConcurrentQueue<(long, IRunnable)>();
        private Queue<(long, IRunnable)> m_RunningQueue = new Queue<(long, IRunnable)>();
        private ConcurrentQueue<IRunnable> m_PendAddRunnables = new ConcurrentQueue<IRunnable>();
        private ConcurrentQueue<IRunnable> m_PendRemoveRunnables = new ConcurrentQueue<IRunnable>();
        private ConcurrentDictionary<long, IRunnable> m_PendRemoveRunnableDict = new ConcurrentDictionary<long, IRunnable>();
        private int m_ThreadWorkerIDGenerator = 0;

        public ThreadCoordinator(int fps)
        {
            m_StandaloneThreadGenerator = 0;
            m_MaxThreadWorkerFramePerSecond = Math.Min(30, Math.Max(10, fps));
            m_MaxThreadWorkerCount = System.Environment.ProcessorCount;

            //NOTE APIServer만 켜지는 경우에는 스레드코디네이터에서 관리되는 스레드를 쓸일이 없다.

            for (int i = 0; i < m_MaxThreadWorkerCount; ++i)
            {
                var newWorkerID = Interlocked.Increment(ref m_ThreadWorkerIDGenerator);
                var newWorker = new ThreadWorker(m_MaxThreadWorkerFramePerSecond, newWorkerID, i == 0 ? eThreadWorkerState.Static : eThreadWorkerState.Free);
                newWorker.Start();
                m_ThreadWorkers.Add(newWorkerID, newWorker);
            }
        }

        public void Initialize()
        {
            AddStandAloneThread(OnUpdate);
        }

        public void TurnBack(long nextTime, IRunnable runnable)
        {
            if (m_PendRemoveRunnableDict.ContainsKey(runnable.GetID()) == true)
            {
                m_PendRemoveRunnableDict.TryRemove(runnable.GetID(), out _);
                runnable.Dispose();
                return;
            }
            m_Runnables.Enqueue((nextTime, runnable));
        }

        public void Clear()
        {
            foreach (var wthread in m_ThreadWorkers.Values)
            {
                wthread.Stop();
            }
            m_ThreadWorkers.Clear();
        }

        public bool AddRunnable(IRunnable runnable)
        {
            if (m_ThreadWorkers.Count > m_MaxThreadWorkerCount)
            {
                return false;
            }
            m_PendAddRunnables.Enqueue(runnable);
            return true;
        }

        public void RemoveRunnable(IRunnable runnable)
        {
            m_PendRemoveRunnables.Enqueue(runnable);
        }

        public int AddStandAloneThread(Command command)
        {
            var newThreadID = Interlocked.Increment(ref m_StandaloneThreadGenerator);
            var newThread = new StandAloneThread(newThreadID, command);
            newThread.Start();
            if (m_StandaloneThreads.TryAdd(newThreadID, newThread) == false)
            {
                newThread.Stop();
                return 0;
            }
            return newThreadID;
        }

        public void RemoveStandaloneThread(int threadID)
        {
            if (m_StandaloneThreads.TryRemove(threadID, out var thread) == false)
            {
                //ERROR
                return;
            }
            thread.Stop();
        }

        private void ExcludeNonAvailableworker()
        {
            var toRemove = new List<int>();
            foreach (var threadWorker in m_ThreadWorkers.Values)
            {
                if (threadWorker.GetState() == eThreadWorkerState.Stopped)
                {
                    toRemove.Add(threadWorker.ThreadWorkerID);
                }
            }
            foreach (var id in toRemove)
            {
                m_ThreadWorkers.Remove(id);
            }
        }
        private ThreadWorker? PeekAvailableThreadWorker(IRunnable runnable)
        {
            if (runnable.RunnableType == RunnableType.System)
            {
                var staticWorker = m_ThreadWorkers.Values.Where(x => x.GetState() == eThreadWorkerState.Static).FirstOrDefault();
                if (staticWorker != null)
                {
                    return staticWorker;
                }
            }
            else if (runnable.RunnableType == RunnableType.Zone || runnable.RunnableType == RunnableType.Bot)
            {
                var freeWorker = m_ThreadWorkers.Values.Where(x => x.GetState() == eThreadWorkerState.Free && x.CanAddRunnable() == true).FirstOrDefault();
                if (freeWorker != null)
                {
                    return freeWorker;
                }
                var normalWorker = m_ThreadWorkers.Values.Where(x => x.GetState() == eThreadWorkerState.Normal && x.CanAddRunnable() == true).FirstOrDefault();
                if (normalWorker != null)
                {
                    return normalWorker;
                }
                var busyWorker = m_ThreadWorkers.Values.Where(x => x.GetState() == eThreadWorkerState.Busy && x.CanAddRunnable() == true).OrderBy(x => x.GetScore()).FirstOrDefault();
                if (busyWorker != null)
                {
                    return busyWorker;
                }
            }
            return null;
        }

        private ThreadWorker CreateNewThreadWorker(IRunnable runnable)
        {
            var newWorkerID = Interlocked.Increment(ref m_ThreadWorkerIDGenerator);
            if (runnable.RunnableType == RunnableType.System)
            {
                var newWorker = new ThreadWorker(m_MaxThreadWorkerFramePerSecond, newWorkerID, eThreadWorkerState.Free);
                newWorker.Start();
                m_ThreadWorkers.Add(newWorkerID, newWorker);
                return newWorker;
            }
            else
            {
                throw new Exception("Invalid new worker Type");
            }
        }
        private ThreadWorker ScheduleThreadWorker(IRunnable runnable)
        {
            var m_CurrentThreadWorker = PeekAvailableThreadWorker(runnable);
            if (m_CurrentThreadWorker == null)
            {
                throw new Exception("Thread not exist anymore");
            }
            return m_CurrentThreadWorker;
        }

        private void RegisterNewThreadWorker(IRunnable runnable)
        {
            int retryCount = 0;
            while (+retryCount <= BaseConsts.MAX_THREADCOORDINATOR_RETRY_COUNT)
            {
                var worker = ScheduleThreadWorker(runnable);
                if (worker.AddRunnable(runnable) == true)
                {
                    return;
                }
                CoreLog.Critical.LogFatal("Runnable registration faild. ThreadCoordinator invalid");
            }
        }

        public void OnUpdate()
        {
            while (m_PendAddRunnables.TryDequeue(out var runnable) == true)
            {
                if (runnable.RunnableType == RunnableType.Pool)
                {
                    m_Runnables.Enqueue((TimeUtil.GetCurrentTickMilliSec(), runnable));
                }
                else
                {
                    RegisterNewThreadWorker(runnable);
                }
            }
            while (m_Runnables.TryDequeue(out var pair) == true)
            {
                m_RunningQueue.Enqueue(pair);
            }
            var currentTime = TimeUtil.GetCurrentTickMilliSec();

            while (m_RunningQueue.Count > 0)
            {
                var pair = m_RunningQueue.Dequeue();

                if (currentTime < pair.Item1)
                {
                    m_Runnables.Enqueue(pair);
                }
            }

            while (m_PendRemoveRunnables.TryDequeue(out var runnable) == true)
            {
                if (runnable.RunnableType == RunnableType.System)
                {
                    if (m_ThreadWorkers.TryGetValue(runnable.ThreadWorkerID, out var threadworker) == false)
                    {
                        throw new Exception("Invalid Threadworker - Not Found");
                    }
                    threadworker.RemoveRunnable(runnable.GetID());
                }
                ExcludeNonAvailableworker();
                ThreadUtil.SleepwithThreadSpinWait(5);

                s_gauge.WithLabels("total_tc_count").Set(m_ThreadWorkers.Count);
                s_gauge.WithLabels("free_tw_count").Set(m_ThreadWorkers.Values.Where(x => x.GetState() == eThreadWorkerState.Free).Count());
                s_gauge.WithLabels("normal_tw_count").Set(m_ThreadWorkers.Values.Where(x => x.GetState() == eThreadWorkerState.Normal).Count());
                s_gauge.WithLabels("busy_tw_count").Set(m_ThreadWorkers.Values.Where(x => x.GetState() == eThreadWorkerState.Busy).Count());
            }
        }

        public long MakeRunnableID()
        {
            return Interlocked.Increment(ref m_runnableIDCounter);
        }
    }
}