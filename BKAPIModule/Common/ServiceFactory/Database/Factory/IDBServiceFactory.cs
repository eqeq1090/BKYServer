using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKWebAPIComponent.Mapper;
using BKWebAPIComponent.Service;
using BKWebAPIComponent.Service.Global;
using BKWebAPIComponent.Service.Initialize;

namespace BKWebAPIComponent.Common.ServiceFactory.Database.Factory
{
    public abstract class IDBServiceFactory
    {
        protected DBQueryMapper m_QueryMapper;
        protected ShardNumCheckService m_ShardNumCheckService;

        public IDBServiceFactory(DBQueryMapper mapper, ShardNumCheckService shardNumCheckService)
        {
            m_QueryMapper = mapper;
            m_ShardNumCheckService = shardNumCheckService;
            RegisterService();
        }

        protected virtual void RegisterService()
        {
        }

        public T? GetService<T>()
            where T : IDBService, new()
        {
            return new T();
        }

        protected T GetMasterDBServiceInternal<T>()
           where T : IDBService, new()
        {
            var resultService = new T();
            //NOTE account DB는 master db 형식을 취하므로 , shard를 고려하지 않는다.
            resultService.SetServiceInfo(BKSchemaType.bk_global_master, shardNum: 0, m_QueryMapper);
            return resultService;
        }

        protected async ValueTask<int> GetShardNum(long playerUID)
        {
            var shardNum = m_ShardNumCheckService.GetShardNum(playerUID);
            if (shardNum != -1)
            {
                return shardNum;
            }
            var shardNumResult = await GetMasterDBServiceInternal<MasterDBBaseService>().GetShardNum(playerUID);
            if (!shardNumResult.IsSuccess())
            {
                CoreLog.Critical.LogError($"GetShardNum failed, playerUID: {playerUID}");
                return -1;
            }
            return shardNumResult.GetResult();
        }
        protected async Task<int> GetShardNumByTag(string playerTag)
        {
            var shardNumResult = await GetMasterDBServiceInternal<MasterDBBaseService>().GetShardNumByTag(playerTag);
            if (!shardNumResult.IsSuccess())
            {
                CoreLog.Critical.LogError($"GetShardNumByTag failed, playerTag: {playerTag}");
                return -1;
            }
            return shardNumResult.GetResult();
        }
    }
}
