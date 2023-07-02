using BKServerBase.Config;
using BKServerBase.ConstEnum;

namespace BKCommonComponent.Redis.Detail
{
    public class RedisKeyGroup
    {
        private const string SessionRedisKey = "SESSION_ID:{0}";
        private const string PresenceRedisKey = "PRESENCE_ID:{0}";
        private const string FriendlyRoomRedisKey = "FRIENDLY_ROOM:{0}";
        private const string ReconnectionRedisKey = "RECONNECTION_ID:{0}";

        public static string MakePresenceKey(long playerUID) => string.Format(PresenceRedisKey, playerUID);
        public static string MakeSessionKey(long playerUID) => string.Format(SessionRedisKey, playerUID);
        public static string MakeReconnectionKey(long playerUID) => string.Format(ReconnectionRedisKey, playerUID);
        public static string MakeFriendlyRoomKey(string roomCode) => string.Format(FriendlyRoomRedisKey, roomCode);

        public static string MakeChannelName(BKRedisDataType redisDataType)
        {
            var channel = ConfigManager.Instance.RedisConnectionConf!.GetRedisClientName(0, redisDataType);
            return channel;
        }

        public static string MakeChannelName(BKRedisDataType redisDataType, string key)
        {
            var clientName = ConfigManager.Instance.RedisConnectionConf!.GetRedisClientName(0, redisDataType);
            var channel = $"{clientName}:{key}";

            return channel;
        }
    }
}
