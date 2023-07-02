using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Util
{
    public static class KeyGenerator
    {
        private static volatile int[] m_counterIndex = new int[2];
        private static byte m_serverId;
        private static readonly DateTime m_baseStartTime = new DateTime(2023, 1, 1, 0, 0, 0, 0);
        private static readonly BitVector64.Section m_serverIdSection;
        private static readonly BitVector64.Section m_counterSection;
        private static readonly BitVector64.Section m_timeSection;

        static KeyGenerator()
        {
            m_serverIdSection = BitVector64.CreateSection(8);
            m_counterSection = BitVector64.CreateSection(20, m_serverIdSection);
            m_timeSection = BitVector64.CreateSection(31, m_counterSection);

            for (int i = 0; i < m_counterIndex.Length; ++i)
            {
                m_counterIndex[i] = -1;
            }
        }

        public static void Initialize(int serverId)
        {
            if (serverId <= 0 || serverId > m_serverIdSection.Mask)
            {
                throw new ArgumentException($"SetServerId failed, invalid serverId: {serverId}");
            }

            m_serverId = (byte)serverId;
        }

        public static long Issue()
        {
            var bitVector = new BitVector64();
            bitVector[m_timeSection] = (uint)DateTime.Now.Subtract(m_baseStartTime).TotalSeconds;
            bitVector[m_counterSection] = Interlocked.Increment(ref m_counterIndex[0]) & m_counterSection.Mask;
            bitVector[m_serverIdSection] = m_serverId;

            return bitVector.Data;
        }

        public static long PacketUID()
        {
            var bitVector = new BitVector64();
            bitVector[m_timeSection] = (uint)DateTime.Now.Subtract(m_baseStartTime).TotalSeconds;
            bitVector[m_counterSection] = Interlocked.Increment(ref m_counterIndex[1]) & m_counterSection.Mask;
            bitVector[m_serverIdSection] = m_serverId;

            return bitVector.Data;
        }
    }
}
