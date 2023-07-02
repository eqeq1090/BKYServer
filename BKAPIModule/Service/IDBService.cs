using System.Collections.Concurrent;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Util;
using BKCommonComponent.Redis.Detail;
using BKWebAPIComponent.Common.DBSession;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.Mapper;
using BKWebAPIComponent.Service.Detail;
using BKWebAPIComponent.Service.Player;

namespace BKWebAPIComponent.Service
{
    public abstract class IDBService
    {
        //private static readonly Dictionary<SchemaType, List<ShardPoolingInfo>> m_SessionPoolMap = new();
        //private static readonly ConcurrentQueue<PendingRequestInfo<DBSession>> m_PendingRequests = new();
        // TODO: Connection을 얻는 방식의 재귀적인 문제를 해결되면 다시 복구.

        private int m_ShardNum;
        private BKSchemaType m_SchemaType;
        protected string m_ConnectionString = string.Empty;
        protected DBQueryMapper? m_QueryMapper;

        public IDBService()
        {

        }

        //public static void Return(DBSession session)
        //{
        //    session.Reuse();

        //    if (m_PendingRequests.TryDequeue(out var pendingRequest))
        //    {
        //        session.OpenAsync()
        //            .ContinueWith(_ =>
        //            {
        //                pendingRequest.Resolove(session);
        //            });
        //        return;
        //    }

        //    session.Return();
        //}

        //public static void Initialize(DBQueryMapper queryMapper)
        //{
        //    foreach (var shardInfo in ConfigManager.Instance.DBConnectionConf!.Shards)
        //    {
        //        m_SessionPoolMap.Add(shardInfo.Key, new List<ShardPoolingInfo>());

        //        foreach (var connectionInfo in shardInfo.Value.Connections)
        //        {
        //            var connectionString = connectionInfo.GetConnectionString();

        //            var shardPoolingInfo = new ShardPoolingInfo(
        //                minShardNum: connectionInfo.ShardMin,
        //                maxShardNum: connectionInfo.ShardMax);

        //            for (int i = 0; i < ConfigManager.Instance.DBConnectionConf!.DbSessionPoolingSize; ++i)
        //            {
        //                shardPoolingInfo.Add(new DBSession(
        //                    connectionString, 
        //                    queryMapper));
        //            }
                    
        //            m_SessionPoolMap[shardInfo.Key].Add(shardPoolingInfo);
        //        }
        //    }
        //}

        public void SetServiceInfo(BKSchemaType schemaType, int shardNum, DBQueryMapper mapper)
        {
            var connectionString = ConfigManager.Instance.DBConnectionConf!.GetShardConnectionString(schemaType, shardNum);

            m_ShardNum = shardNum;
            m_SchemaType = schemaType;
            m_ConnectionString = connectionString;
            m_QueryMapper = mapper;
        }

        public async Task<DBTransaction> GetDBTransaction()
        {
            if (m_ConnectionString == string.Empty || m_QueryMapper == null)
            {
                throw new Exception("DB Service Base Info not Initialized");
            }
            var dbTransaction = new DBTransaction(m_ConnectionString, m_QueryMapper);
            await dbTransaction.OpenAsync(ConfigManager.Instance.DBConnectionConf!.IsolationLevel);

            return dbTransaction;
        }

        public async Task<DBSession> GetDBSession()
        {
            if (m_ConnectionString == string.Empty || m_QueryMapper == null)
            {
                throw new Exception("DB Service Base Info not Initialized");
            }

            //if (m_SessionPoolMap.ContainsKey(m_SchemaType) is false)
            //{
            //    throw new Exception($"not supported schemaType: {m_SchemaType}");
            //}

            //var shardPoolingInfo = m_SessionPoolMap[m_SchemaType]
            //    .FirstOrDefault(e => m_ShardNum >= e.MinShardNum && m_ShardNum <= e.MaxShardNum);
            //if (shardPoolingInfo is null)
            //{
            //    throw new Exception($"cant find sharding session, shardNum: {m_ShardNum}");
            //}

            //var dbSession = shardPoolingInfo.Take();
            //if (dbSession is null)
            //{
            //    var tcs = new TaskCompletionSource<DBSession>();
            //    m_PendingRequests.Enqueue(new PendingRequestInfo<DBSession>(tcs));

            //    dbSession = await tcs.Task;
            //    return dbSession;
            //}

            var dbSession = new DBSession(m_ConnectionString, m_QueryMapper);
            await dbSession.OpenAsync();
            return dbSession;
        }

        public T CastTo<T>()
            where T : PlayerBaseService, new()
        {
            if (m_ShardNum <= 0)
            {
                throw new Exception($"Cast failed, shardNum is empty");
            }

            if (m_QueryMapper is null)
            {
                throw new Exception($"CastTo failed, QueryMapper is null");
            }

            var resultService = new T();
            resultService.SetServiceInfo(m_SchemaType, m_ShardNum, m_QueryMapper);
            return resultService;
        }
    }
}
