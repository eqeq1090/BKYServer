using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Component;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;

namespace BKCommonComponent.Redis
{
    public readonly struct RedisOperator
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings();

        public RedisOperator()
        {
        }

        private IRedisComponent RedisComponent => ComponentManager.Instance.GetComponent<IRedisComponent>()
            ?? throw new Exception($"RedisCompoent is not registered in ComponetManager");

        public async Task<T?> GetAsync<T>(BKRedisDataType redisDataType, RedisKey key, TimeSpan? expiry)
            where T : class
        {
            try
            {
                using var redisClient = await RedisComponent.GetClientAsync(redisDataType);
                var database = redisClient.GetDatabase(0);
                if (database is null)
                {
                    CoreLog.Critical.LogError($"GetDatabase failed, database is null");
                    return null;
                }
                
                var redisValue = await database.StringGetSetExpiryAsync(key, expiry: expiry, CommandFlags.None);
                if (redisValue.IsNull)
                {
                    // CoreLog.Normal.LogWarning($"redisValue is null, key: {key}");
                    return null;
                }

                return JsonConvert.DeserializeObject<T>(redisValue!, JsonSettings);
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"RedisOperator GetAsync failed, exception: {e}");
                return null;
            }
        }

        public async Task<string?> GetStringAsync(BKRedisDataType redisDataType, RedisKey key, TimeSpan? expiry)
        {
            try
            {
                using var redisClient = await RedisComponent.GetClientAsync(redisDataType);
                var database = redisClient.GetDatabase(0);
                if (database is null)
                {
                    CoreLog.Critical.LogError($"GetDatabase failed, database is null");
                    return null;
                }

                var redisValue = await database.StringGetSetExpiryAsync(key, expiry: expiry, CommandFlags.None);
                if (redisValue.IsNull)
                {
                    // CoreLog.Critical.LogError($"redisValue is null, key: {key}");
                    return null;
                }

                return redisValue.ToString();
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"RedisOperator GetAsync failed, exception: {e}");
                return null;
            }
        }

        public async Task<T?[]?> GetAllAsync<T>(BKRedisDataType redisDataType, RedisKey[] keys)
            where T : class
        {
            try
            {
                using var redisClient = await RedisComponent.GetClientAsync(redisDataType);
                var database = redisClient.GetDatabase(0);
                if (database is null)
                {
                    CoreLog.Critical.LogError($"GetDatabase failed, database is null");
                    return null;
                }

                var redisValues = await database.StringGetAsync(keys, CommandFlags.None);
                var result = redisValues.Select(e =>
                {
                    if (e.IsNull)
                    {
                        return null;
                    }

                    return JsonConvert.DeserializeObject<T>(e!, JsonSettings);
                }).ToArray();

                return result;
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"RedisOperator GetAllAsync failed, exception: {e}");
                return null;
            }
        }

        public async Task<bool> AddAsync<T>(BKRedisDataType redisDataType, RedisKey key, T data, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
            where T : class
        {
            try
            {
                using var redisClient = await RedisComponent.GetClientAsync(redisDataType);
                var database = redisClient.GetDatabase(0);
                if (database is null)
                {
                    CoreLog.Critical.LogError($"GetDatabase failed, database is null");
                    return false;
                }

                var json = JsonConvert.SerializeObject(data);
                return await database.StringSetAsync(key, json, expiry: expiry, when: When.Always, flags: flags);
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"RedisOperator AddAsync failed, exception: {e}");
                return false;
            }
        }

        public async Task<bool> AddAsync<T>(IDatabase database, RedisKey key, T data, TimeSpan? expiry, CommandFlags flags)
            where T : class
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                return await database.StringSetAsync(key, json, expiry: expiry, when: When.Always, flags: flags);
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"RedisOperator AddAsync failed, exception: {e}");
                return false;
            }
        }

        public async Task<bool> AddStringAsync(BKRedisDataType redisDataType, RedisKey key, string value, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
        {
            try
            {
                using var redisClient = await RedisComponent.GetClientAsync(redisDataType);
                var database = redisClient.GetDatabase(0);
                if (database is null)
                {
                    CoreLog.Critical.LogError($"GetDatabase failed, database is null");
                    return false;
                }

                return await database.StringSetAsync(key, value, expiry: expiry, when: When.Always, flags: flags);
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"RedisOperator AddStringAsync failed, exception: {e}");
                return false;
            }
        }

        public async Task<bool> AddStringAsync(IDatabase database, RedisKey key, string value, TimeSpan? expiry, CommandFlags flags)
        {
            try
            {
                return await database.StringSetAsync(key, value, expiry: expiry, when: When.Always, flags: flags);
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"RedisOperator AddStringAsync failed, exception: {e}");
                return false;
            }
        }

        public async Task<bool> RemoveKeyAsync(BKRedisDataType redisDataType, RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            try
            {
                using var redisClient = await RedisComponent.GetClientAsync(redisDataType);
                var database = redisClient.GetDatabase(0);
                if (database is null)
                {
                    CoreLog.Critical.LogError($"GetDatabase failed, database is null");
                    return false;
                }

                return await database.KeyDeleteAsync(key, flags: flags);
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"RedisOperator RemoveKeyAsync failed, exception: {e}");
                return false;
            }
        }

        public async Task<bool> KeyExpireAsync(BKRedisDataType redisDataType, RedisKey key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
        {
            try
            {
                using var redisClient = await RedisComponent.GetClientAsync(redisDataType);
                var database = redisClient.GetDatabase(0);
                if (database is null)
                {
                    CoreLog.Critical.LogError($"GetDatabase failed, database is null");
                    return false;
                }

                return await database.KeyExpireAsync(key, expiry, when: ExpireWhen.Always, flags: flags);
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"RedisOperator KeyExpiry failed, exception: {e}");
                return false;
            }
        }
    }
}
