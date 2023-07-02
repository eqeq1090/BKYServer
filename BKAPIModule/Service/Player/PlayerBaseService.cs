
using BKServerBase.ConstEnum;
using BKServerBase.Extension;
using BKServerBase.Util;
using BKDataLoader.MasterData;
using BKProtocol;
using BKProtocol.Enum;
using BKWebAPIComponent.Common.DBSession;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.Model.Common;
using BKWebAPIComponent.Model.Entity;
using BKWebAPIComponent.Model.Entity.PlayerShard;
using static BKWebAPIComponent.Mapper.DBQueryMapper;

namespace BKWebAPIComponent.Service.Player
{
    public class PlayerBaseService : IDBService
    {
        public async Task<ServiceResult<ShardPlayerEntity>> GetPlayerInternal(AbstractDBSession session, long playerUID)
        {
            var getResult = await session.ExecuteQuery<ShardPlayerEntity, PlayerUIDEntity>(QueryDef.GetShardPlayer, new PlayerUIDEntity()
            {
                PlayerUID = playerUID
            });
            if (getResult.IsSuccess() == false)
            {
                return new ServiceResult<ShardPlayerEntity>(getResult.GetErrorCode());
            }
            return new ServiceResult<ShardPlayerEntity>(ConstEnum.ServiceErrorCode.SUCCESS, getResult.GetResult());
        }
    //플레이어 전체

    }
}
