using ConcurrentCollections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using BKServerBase.Util;

namespace BKServerBase.Timer
{
    public delegate void OnSlotMove(long interval);

    public sealed class TimerWheel
    {
        private const int IntervalMillisec = 5;
        private long m_LastTick;
        private readonly ConcurrentQueue<SlotTask>[] buckets;
        private long m_CurrentSlotPos;
        private Thread m_Thread;
        private OnSlotMove m_OnSlotMove;
        private int m_SlotSize;
        private int m_FrameInterval;
        private ConcurrentHashSet<long> m_RemovePendedSlotTaskKey = new ConcurrentHashSet<long>();

        public TimerWheel(int slotSize, int frameTimeLength)
        { 
            if (slotSize <= 0)
            {
                throw new Exception($"Invalid SlotSize TimerWheel. {slotSize}");
            }
            buckets = new ConcurrentQueue<SlotTask>[slotSize];
            for (int i = 0; i < buckets.Length; ++i)
            {
                buckets[i] = new ConcurrentQueue<SlotTask>();
            }
            m_OnSlotMove += OnSlotMove;
            m_SlotSize = slotSize;
            m_FrameInterval = frameTimeLength / IntervalMillisec;
            m_Thread = new Thread(Run);
        }

        public void Start()
        {
            m_LastTick = TimeUtil.GetCurrentTickMilliSec();
            m_Thread.Start();
        }

        private void Run()
        {
            while (true)
            {
                var currentTick = TimeUtil.GetCurrentTickMilliSec();
                var interval = currentTick - m_LastTick;
                if (interval < IntervalMillisec)
                {
                    Thread.SpinWait((int)(IntervalMillisec - interval));
                    continue;
                }
                m_LastTick = currentTick;

                m_OnSlotMove.Invoke(interval);
            }
        }

        private void OnSlotMove(long deltaMillisec)
        {
            Interlocked.Exchange(ref m_CurrentSlotPos, m_CurrentSlotPos == m_SlotSize - 1 ? 0 : m_CurrentSlotPos + 1);

            var commands = buckets[m_CurrentSlotPos];

            while (commands.TryDequeue(out var command))
            {
                if (m_RemovePendedSlotTaskKey.Contains(command.Key) == true)
                {
                    m_RemovePendedSlotTaskKey.TryRemove(command.Key);
                    continue;
                }
                var interval = TimeUtil.GetCurrentTickMilliSec() - command.LastTick;
                BackgroundJob.Execute(() =>
                {
                    try
                    {
                        var startTime = TimeUtil.GetCurrentTickMilliSec();
                        command.RunAction(interval);
                        var logicInterval = TimeUtil.GetCurrentTickDiffMilliSec(startTime);

                        if (command.IsRepeat)
                        {
                            var calcedSlotCount = (logicInterval / IntervalMillisec);
                            if (calcedSlotCount < 0) 
                            {
                                throw new Exception($"calcedSlotCount must be greater than . {calcedSlotCount}");
                            }
                            AllocateSlot(command, m_FrameInterval - calcedSlotCount <= 0 ? 1 : m_FrameInterval - calcedSlotCount);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e);
                    }
                });
            }
        }

        public long AddTask(bool isRepeat, Action<long> action)
        {
            var task = new SlotTask(isRepeat, action);

            AllocateSlot(task, 1);

            return task.Key;
        }

        public void RemoveTask(long key)
        {
            m_RemovePendedSlotTaskKey.Add(key);
        }

        private void AllocateSlot(SlotTask command, long slotCount)
        {
            var currentPos = Interlocked.Read(ref m_CurrentSlotPos);

            if (slotCount <= 0)
            {
                throw new Exception($"slot count must be greater than 1. input : {slotCount}");
            }

            long index = (currentPos + slotCount) % m_SlotSize;

            buckets[index].Enqueue(command);
        }

        public class SlotTask
        {
            public Action<long> Action { get; }
            public bool IsRepeat { get; }
            public long Key { get; }
            public long LastTick { get; private set; }

            public SlotTask(bool isRepeat, Action<long> action)
            {
                Key = KeyGenerator.Issue();
                IsRepeat = isRepeat;
                Action = action;
                LastTick = TimeUtil.GetCurrentTickMilliSec();
            }

            public void RunAction(long input)
            {
                Action(input);
                LastTick = TimeUtil.GetCurrentTickMilliSec();
            }
        }
    }
}
