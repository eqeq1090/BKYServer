using System.Collections.Concurrent;
using BKWebAPIComponent.Common.DBSession;

namespace BKWebAPIComponent.Service.Detail
{
    public sealed class ShardPoolingInfo
    {
        private readonly ConcurrentBag<DBSession> m_SessionPooling = new();

        public ShardPoolingInfo(int minShardNum, int maxShardNum)
        {
            MinShardNum = minShardNum;
            MaxShardNum = maxShardNum;
        }

        public void Add(DBSession session)
        {
            m_SessionPooling.Add(session);
        }

        public DBSession? Take()
        {
            m_SessionPooling.TryTake(out var session);
            return session;
        }

        public int MinShardNum { get; }
        public int MaxShardNum { get; }
    }
}
