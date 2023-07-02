using Amazon.S3.Model.Internal.MarshallTransformations;
using BKDataLoader.MasterData;
using BKProtocol;
using BKWebAPIComponent.Model.Entity.Composite;
using BKWebAPIComponent.Model.Entity.PlayerShard;

namespace BKWebAPIComponent.Common.Util
{
    public static class EntityToIMsg
    {
        public static PlayerInfo ToPlayerInfo(PlayerAllInfoEntity entity)
        {
            var result = new PlayerInfo()
            {
                playerUID = entity.ShardPlayer.PlayerUID,
                name = entity.ShardPlayer.Name,
                playerTag = entity.ShardPlayer.PlayerTag,
                //TODO(veck01) 나중에 채워주세요.
                //matchInfo = ,
                //photonResion = ,
                //
            };
            return result;
        }
    }
}
