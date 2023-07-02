using Mysqlx.Crud;
using System.Threading.Tasks;
using BKServerBase.Util;
using BKDataLoader.MasterData;
using BKProtocol;
using BKProtocol.Enum;
using BKWebAPIComponent.Common.DBSession;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.Common.Util;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.Model.Common;
using BKWebAPIComponent.Model.Entity;
using BKWebAPIComponent.Model.Entity.Composite;
using BKWebAPIComponent.Model.Entity.Composite.Result;
using BKWebAPIComponent.Model.Entity.PlayerShard;
using static org.apache.zookeeper.OpResult;
using static BKWebAPIComponent.Mapper.DBQueryMapper;

namespace BKWebAPIComponent.Service.Player
{
    public class PlayerService : PlayerBaseService
    {
        public async Task<ServiceResult<ShardPlayerEntity>> CreatePlayerInternal(AbstractDBSession session, long playerID, string tag)
        {
            //NOTE 기본 player table 추가
            var createResult = await session.ExecuteQuery<VoidEntity, ShardPlayerEntity>(QueryDef.InsertShardPlayer, new ShardPlayerEntity()
            {
                PlayerUID = playerID,
                Name = ConstEnum.Consts.DEFAULT_NAME,
                PlayerTag = tag,
            });
            if (createResult.IsSuccess() == false)
            {
                return new ServiceResult<ShardPlayerEntity>(createResult.GetErrorCode());
            }

            var getResult = await session.ExecuteQuery<ShardPlayerEntity, PlayerUIDEntity>(QueryDef.GetShardPlayer, new PlayerUIDEntity()
            {
                PlayerUID = playerID
            });
            if (getResult.IsSuccess() == false)
            {
                return new ServiceResult<ShardPlayerEntity>(createResult.GetErrorCode());
            }

            return new ServiceResult<ShardPlayerEntity>(ConstEnum.ServiceErrorCode.SUCCESS, getResult.GetResult());
        }

        public async Task<ServiceResult<ShardPlayerEntity>> GetPlayer(long playerUID)
        {
            using (var session = await GetDBSession())
            {
                return await GetPlayerInternal(session, playerUID);
            }
        }
    }
}
