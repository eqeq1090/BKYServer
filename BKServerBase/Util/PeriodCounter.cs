using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace BKServerBase.Util
{
    public class PeriodCounter
    {
        private int[] m_timeSlots;
        private long m_baseTick;
        private int m_baseSlot;

        public PeriodCounter(int period)
        {
            m_timeSlots = new int[period];
            m_baseTick = 0;
            m_baseSlot = 0;
        }

        public void Reset(long currentTick)
        {
            m_baseTick = currentTick;
            m_baseSlot = 0;

            for (int i = 0; i < m_timeSlots.Length; i++)
            {
                m_timeSlots[i] = 0;
            }
        }

        private void Update(long currentTick)
        {
            var deltaTick = (currentTick - m_baseTick);
            if (m_baseTick > currentTick || m_timeSlots.Length <= deltaTick)
            {
                Reset(currentTick);
                return;
            }
            for (int i = m_baseSlot + 1; i <= m_baseSlot + deltaTick; i++)
            {
                m_timeSlots[i % m_timeSlots.Length] = 0;
            }
            m_baseSlot = (m_baseSlot + (int)deltaTick) % m_timeSlots.Length;
            m_baseTick += deltaTick;
        }
        public int Increment(long currentTick, int value = 1)
        {
            Update(currentTick);
            m_timeSlots[m_baseSlot] += value;
            return m_timeSlots.Sum();
        }

        public int Get(long currentTick)
        {
            Update(currentTick);
            return m_timeSlots.Sum();
        }
    }
}