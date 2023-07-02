using BKServerBase.Util;

namespace BKServerBase.Threading
{
    public class FramePerSecond
    {
        public uint m_FramesPerSec { get; private set; }
        public uint m_FrameCount { get; private set; }
        public uint m_LoopCount = 0;
        public uint m_FrameCheckCount = 0;
        public long m_MostDelayedTime { get; private set; }
        public long m_FrameTime { get; private set; }
        public long m_LastTime { get; private set; }
        public static long CurrentMilliSecTick { get => TimeUtil.GetCurrentTickMilliSec(); }
        public uint CurrentFramePerSecond { get => m_FramesPerSec; }
        public long GetMaxDelayTime { get => m_MostDelayedTime; }
        public long GetGapFromLastTime { get => CurrentMilliSecTick - m_LastTime; }
        public long MostDelayedTime { get => m_MostDelayedTime; set => m_MostDelayedTime = value; }
        public uint Loopcount { get => m_LoopCount; set => m_LoopCount = value; }
        public FramePerSecond()
        {
            m_FramesPerSec = 0;
            m_FrameCount = 0;
            m_LoopCount = 0;
            m_FrameCheckCount = 0;
            m_MostDelayedTime = 0;
            m_FrameTime = TimeUtil.GetCurrentTickMilliSec();
            m_LastTime = m_FrameTime;
        }

        public void CalcFramePerSecond()
        {
            long currentTime = CurrentMilliSecTick;
            long deltaTime = currentTime - m_FrameTime;
            m_FrameCount++;
            if (currentTime - m_LastTime > m_MostDelayedTime)
            {
                m_MostDelayedTime = currentTime - m_LastTime;
            }
            m_LastTime = currentTime;
            if (deltaTime > 1000)
            {
                m_FrameTime = currentTime;
                m_FramesPerSec = (m_FrameCount * 1000) / (uint)deltaTime;
                m_FrameCount = 0;
                m_MostDelayedTime = 0;
            }
            m_LoopCount++;
        }
    }
}
