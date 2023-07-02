using StackExchange.Redis;
using BKServerBase.ConstEnum;
using System.Collections.Immutable;
using CommandLine;
using BKCommonComponent.Redis;
using BKCommonComponent.Redis.Detail;
using BKServerBase.Component;
using BKProtocol;

namespace BKWebAPIComponent.Redis
{
    public class RedisDataService
    {
        public RedisDataService()
        {
        }

        public async Task<PresenceData?> GetPresenceDataAsync(long playerUID)
        {
            var key = RedisKeyGroup.MakePresenceKey(playerUID);
            var redisOperator = new RedisOperator();

            return await redisOperator.GetAsync<PresenceData>(BKRedisDataType.presence, key, expiry: null);
        }

        public async Task<ImmutableDictionary<long, PresenceData>?> GetPresenceDataMapAsync(IEnumerable<long> playerUIDs)
        {
            var redisOperator = new RedisOperator();
            var keys = playerUIDs
                .Select(e => (RedisKey)RedisKeyGroup.MakePresenceKey(e))
                .ToArray();

            var result = await redisOperator.GetAllAsync<PresenceData>(BKRedisDataType.presence, keys);
            if (result is null)
            {
                return null;
            }

            return result
                .Where(e => e != null)
                .Cast<PresenceData>()
                .ToImmutableDictionary(e => e.PlayerUID);
        }
        
        public async Task<bool> AddPresenceDataAsync(long playerUID, PresenceData presenceData)
        {
            var redisOperator = new RedisOperator();
            var key = RedisKeyGroup.MakePresenceKey(playerUID);

            return await redisOperator.AddAsync<PresenceData>(BKRedisDataType.presence, key, presenceData, expiry: null);
        }

        public async Task<string?> GetSessionIDAsync(long playerUID)
        {
            var redisOperator = new RedisOperator();
            var key = RedisKeyGroup.MakeSessionKey(playerUID);

            return await redisOperator.GetStringAsync(BKRedisDataType.session, key, expiry: null);
        }

        public async Task<bool> AddSessionIDAsync(long playerUID, string sessionID)
        {
            var redisOperator = new RedisOperator();
            var key = RedisKeyGroup.MakeSessionKey(playerUID);

            return await redisOperator.AddStringAsync(BKRedisDataType.session, key, sessionID, expiry: null, CommandFlags.None);
        }

        public async Task ExpirePresenceDataAsync(long playerUID, TimeSpan? expiry)
        {
            var redisOperator = new RedisOperator();
            var key = RedisKeyGroup.MakePresenceKey(playerUID);
            await redisOperator.KeyExpireAsync(BKRedisDataType.presence, key, expiry, CommandFlags.FireAndForget);
        }

        public async Task RemoveSessionIDAsync(long playerUID)
        {
            var redisOperator = new RedisOperator();
            var key = RedisKeyGroup.MakeSessionKey(playerUID);
            await redisOperator.RemoveKeyAsync(BKRedisDataType.session, key, CommandFlags.FireAndForget);
        }
    }
}
