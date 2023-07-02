using BKServerBase.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using Prometheus;

namespace BKServerBase.Threading
{
    public delegate void OnTimerHandler();

    public class EventTimer : IDisposable
    {
        public class STimer
        {
            public long LastEvent = 0;
            public long Duration = 0;
            public bool RepeatMode = false;
            public OnTimerHandler? onTimerAlarm = null;
            public int TimerID = 0;
            public string? TimerName;
            public long ElapsedTime { get { return TimeUtil.GetCurrentTickMilliSec() - LastEvent; } }
            public int GetRemainTime()
            {
                return (int)((Duration - ElapsedTime) * 0.001);
            }

            public long GetEndTime()
            {
                return TimeUtil.GetCurrentTickMilliSec() + (Duration - ElapsedTime);
            }
        }

        public int TimeIDInc = 1;
        private Dictionary<string, int> TimerNameDict = new Dictionary<string, int>();
        private Dictionary<int, STimer> Timers = new Dictionary<int, STimer>();
        private bool disposedValue;
        private static readonly Counter s_counter = Metrics
        .CreateCounter("s3 game event timer", "Event Timer", "kind");

        public EventTimer()
        {
            ObjectCounter<EventTimer>.Increment();
        }
        ~EventTimer()
        {
            Dispose(disposing: false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Clear();
                }
                ObjectCounter<EventTimer>.Decrement();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            Timers.Clear();
            TimerNameDict.Clear();
        }

        public void Update()
        {
            var toFireTimer = new List<STimer>();
            foreach (var item in Timers)
            {
                if (item.Value.ElapsedTime > item.Value.Duration)
                {
                    toFireTimer.Add(item.Value);
                }
            }
            foreach (var item in toFireTimer)
            {
                if (item.RepeatMode == false)
                {
                    var timerName = item.TimerName;
                    if (timerName != null)
                    {
                        TimerNameDict.Remove(timerName);
                    }
                    Timers.Remove(item.TimerID);
                }
                item.onTimerAlarm?.Invoke();
                item.LastEvent = TimeUtil.GetCurrentTickMilliSec();
            }
            //s_counter.WithLabels("fire count total").Inc(toFireTimer.Count);
        }

        public int CreateTimerEvent(OnTimerHandler onTimerHandler,
        long duration,
        bool repeat = false,
        string? timerName = null)
        {
            STimer timer = new STimer()
            {
                LastEvent = TimeUtil.GetCurrentTickMilliSec(),
                onTimerAlarm = onTimerHandler,
                Duration = duration,
                RepeatMode = repeat,
                TimerID = Interlocked.Increment(ref TimeIDInc),
                TimerName = timerName
            };
            if (Timers.TryAdd(timer.TimerID, timer) == false)
            {
                return -1;
            }
            if (timerName != null)
            {
                TimerNameDict.TryAdd(timerName, timer.TimerID);
            }
            return timer.TimerID;
        }

        public bool RemoveTimerEvent(string timerName)
        {
            if (TimerNameDict.TryGetValue(timerName, out var ID) == false)
                return false;
            Timers.Remove(ID);
            TimerNameDict.Remove(timerName);
            return true;
        }

        public bool RemoveTimerEvent(int timerID)
        {
            if (Timers.TryGetValue(timerID, out var timer) == false)
            {
                return false;
            }
            Timers.Remove(timer.TimerID);
            if (timer.TimerName != null)
            {
                TimerNameDict.Remove(timer.TimerName);
            }
            return true;
        }

        public bool Exist(string timerName)
        {
            return TimerNameDict.ContainsKey(timerName);
        }

        public int GetRemainTime(string timerName)
        {
            if (TimerNameDict.TryGetValue(timerName, out var ID) == false)
            {
                return 0;
            }
            if (Timers.TryGetValue(ID, out var timer) == false)
            {
                return 0;
            }
            return timer.GetRemainTime();
        }

        public long GetEndTime(string timerName)
        {
            if (TimerNameDict.TryGetValue(timerName, out var ID) == false)
            {
                return 0;
            }
            if (Timers.TryGetValue(ID, out var timer) == false)
            {
                return 0;
            }
            return timer.GetEndTime();
        }

        public long GetDuration(string timerName)
        {
            if (TimerNameDict.TryGetValue(timerName, out var ID) == false)
            {
                return 0;
            }

            if (Timers.TryGetValue(ID, out var timer) == false)
            {
                return 0;
            }
            return timer.Duration;
        }
    }
}