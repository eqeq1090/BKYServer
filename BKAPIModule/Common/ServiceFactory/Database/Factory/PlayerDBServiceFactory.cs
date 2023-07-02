using BKServerBase.ConstEnum;
using BKWebAPIComponent.Mapper;
using BKWebAPIComponent.Service;
using BKWebAPIComponent.Service.Initialize;
using BKWebAPIComponent.Service.Player;

namespace BKWebAPIComponent.Common.ServiceFactory.Database.Factory
{
    public class PlayerDBServiceFactory : IDBServiceFactory
    {
        public PlayerDBServiceFactory(DBQueryMapper mapper, ShardNumCheckService shardNumCheckService)
            : base(mapper, shardNumCheckService)
        {

        }

        public async Task<T> GetPlayerShardService<T>(long playerUID)
            where T : PlayerBaseService, new()
        {
            var shardNum = await GetShardNum(playerUID);
            var resultService = new T();
            resultService.SetServiceInfo(BKSchemaType.bk_player_shard, shardNum, m_QueryMapper);
            return resultService;
        }

        public async Task<T> GetPlayerShardServiceByTag<T>(string playerTag)
            where T : PlayerBaseService, new()
        {
            var shardNum = await GetShardNumByTag(playerTag);
            var resultService = new T();
            resultService.SetServiceInfo(BKSchemaType.bk_player_shard, shardNum, m_QueryMapper);
            return resultService;
        }
    }
}
