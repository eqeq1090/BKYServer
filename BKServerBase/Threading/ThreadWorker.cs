using BKServerBase.Logger;
using BKServerBase.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BKServerBase.ConstEnum;
using BKServerBase.Interface;

namespace BKServerBase.Threading
{
    public class ThreadWorker
    {
        public readonly int MaxFramePerSecond;
        public readonly int ThreadWorkerID;
        protected Dictionary<long, IRunnable> m_Runnables = new Dictionary<long, IRunnable>();
        protected ConcurrentQueue<IRunnable> m_PendAddRunnables = new ConcurrentQueue<IRunnable>();
        protected ConcurrentQueue<long> m_PendRemoveRunnables = new ConcurrentQueue<long>();
        protected Thread? ThreadHandle = null;
        protected FramePerSecond CurrentFPS = new FramePerSecond();
        private eThreadWorkerState m_State;
        private int m_Score = 0;

        public ThreadWorker(int fps, int workerID, eThreadWorkerState initState)
        {
            MaxFramePerSecond = fps;
            ThreadWorkerID = workerID;
            m_State = initState;
        }

        public void Start()
        {
            ThreadHandle = new Thread(ProcCommandPumping)
            {
                Name = $"ThreadWorker-(ThreadworkerID)"
            };
            ThreadHandle.Start();
        }

        public bool AddRunnable(IRunnable runnable)
        {
            m_PendAddRunnables.Enqueue(runnable);
            return true;
        }

        public bool CanAddRunnable()
        {
            return m_State != eThreadWorkerState.Stopped;
        }

        public void RemoveRunnable(long channelID)
        {
            m_PendRemoveRunnables.Enqueue(channelID);
        }

        public eThreadWorkerState GetState()
        {
            return m_State;
        }

        public void Stop()
        {
            m_State = eThreadWorkerState.Stopped;
            CoreLog.Normal.LogDebug(String.Format("ThreadWorker [(0)] Stopped", ThreadWorkerID));
            ThreadHandle?.Join();
        }

        public int GetScore()
        {
            return m_Score;
        }

        public void ProcCommandPumping()
        {
            var stopWatch = new Stopwatch();
            while (m_State != eThreadWorkerState.Stopped)
            {
                stopWatch.Reset();
                stopWatch.Start();
                try
                {
                    while (m_PendRemoveRunnables.TryDequeue(out var channelID))
                    {
                        if (m_Runnables.TryGetValue(channelID, out var runnable) == false)
                        {
                            CoreLog.Critical.LogFatal($"Can't Find Channel in ThreadWorker. Channel ID : (channelID)");
                            continue;
                        }
                        runnable.Dispose();
                        m_Runnables.Remove(channelID);
                    }
                    while (m_PendAddRunnables.TryDequeue(out var runnable))
                    {
                        runnable.SetThread(Thread.CurrentThread, ThreadWorkerID);
                        m_Runnables.Add(runnable.GetID(), runnable);
                    }
                    m_Score = m_Runnables.Values.Sum(x => x.GetScore());
                    m_State = m_State == eThreadWorkerState.Static ? eThreadWorkerState.Static : m_Score switch
                    {
                        < 500 => eThreadWorkerState.Free,
                        < 1000 => eThreadWorkerState.Normal,
                        >= 1000 => eThreadWorkerState.Busy,
                    };
                    foreach (var runnable in m_Runnables.Values)
                    {
                        runnable.OnUpdate();
                    }
                }
                catch (Exception e)
                {
                    CoreLog.Critical.LogFatal(e);
                }
                stopWatch.Stop();
                CurrentFPS.CalcFramePerSecond();
                SleepOnFrameRate(MaxFramePerSecond, stopWatch.ElapsedMilliseconds);
            }
            m_Runnables.Clear();
            while (m_PendAddRunnables.TryDequeue(out _));
            while (m_PendRemoveRunnables.TryDequeue(out _));
        }

        private static void SleepOnFrameRate(int fps, long elapsedTimeMsec)
        {
            if (0 == fps)
            {
                fps = 1;
            }
            long sleepTimeMsec = (long)((1.0f / fps) * 1000);
            long remainTimeMsec = sleepTimeMsec - elapsedTimeMsec;
            if (remainTimeMsec > 0)
            {
                Thread.Sleep((int)remainTimeMsec);
            }
        }
    }
}
