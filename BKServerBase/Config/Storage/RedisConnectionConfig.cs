using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Util;

namespace BKServerBase.Config.Storage
{
    public sealed class RedisConnectionConfig
    {
        public ImmutableDictionary<BKRedisDataType, RedisShardInfo> Shards;

        public RedisConnectionConfig(IConfigurationRoot configRoot)
        {
            var builder = ImmutableDictionary.CreateBuilder<BKRedisDataType, RedisShardInfo>();
            var types = EnumUtil<BKRedisDataType>.GetValues();
            var redisSection = configRoot.GetSection("Redis");
            foreach(var item in types)
            {
                if(item == BKRedisDataType.Invalid)
                {
                    continue;
                }

                var section = redisSection.GetSection(item.ToString());
                if (section.GetChildren().Count() == 0)
                {
                    continue;
                }
                
                var newShardInfo = new RedisShardInfo(item, section);
                builder.Add(newShardInfo.ShardDataType, newShardInfo);
            }
            Shards = builder.ToImmutable();
        }

        public string GetRedisClientName(int shardNum, BKRedisDataType dataType)
        {
            if (Shards.TryGetValue(dataType, out var shard) == false)
            {
                return string.Empty;
            }
            foreach (var connectionInfo in shard.Connections)
            {
                if (connectionInfo.ShardMin <= shardNum && shardNum < connectionInfo.ShardMax)
                {
                    return string.Intern($"{shard.ShardDataType}-{connectionInfo.ID}");
                }
            }
            return string.Empty;
        }
    }

    public sealed class RedisShardInfo
    {
        public readonly BKRedisDataType ShardDataType;
        public readonly int ShardSize;
        public ImmutableList<RedisConnectionInfo> Connections;
        public readonly List<RedisServiceType> ServiceTypes = new List<RedisServiceType>();
        public readonly List<ServerMode> BindServerTypes = new List<ServerMode>();
        public readonly int ConnectionPoolSize;

        public RedisShardInfo(IConfigurationSection section)
        {
            if (Enum.TryParse(section["Type"], out ShardDataType) == false)
            {
                //ERROR
                ShardDataType = BKRedisDataType.Invalid;
            }
            ShardSize = Convert.ToInt32(section["ShardSize"]);

            var bindServerTypesSection = section.GetSection("BindServerTypes");
            if (bindServerTypesSection is null)
            {
                throw new InvalidOperationException($"bindServerTypes is empty");
            }

            foreach (var bindServerType in bindServerTypesSection.GetChildren())
            {
                if (bindServerType is null)
                {
                    throw new Exception($"bindServerType is empty");
                }

                if (Enum.TryParse<ServerMode>(bindServerType.Value, out var serverMode) == false)
                {
                    throw new InvalidDataException($"bindServerType is invalud, value: {bindServerType.Value}");
                }

                BindServerTypes.Add(serverMode);
            }

            var serviceTypesSection = section.GetSection("RedisServiceTypes");
            if (serviceTypesSection is null)
            {
                throw new InvalidDataException($"serviceTypes is empty");
            }

            foreach (var serviceType in serviceTypesSection.GetChildren())
            {
                if (Enum.TryParse<RedisServiceType>(serviceType.Value, out var redisServiceType) == false)
                {
                    throw new InvalidDataException($"redisServiceType is invalid, value: {serviceType.Value}");
                }

                ServiceTypes.Add(redisServiceType);
            }

            var builder = ImmutableList.CreateBuilder<RedisConnectionInfo>();
            foreach (var conn in section.GetSection("Connections").GetChildren())
            {
                var newConn = new RedisConnectionInfo(conn);
                builder.Add(newConn);
            }
            Connections = builder.ToImmutable();

            var connectionPoolSizeSection = section["ConnectionPoolSize"];
            if (connectionPoolSizeSection is null)
            {
                throw new InvalidDataException($"ConnectionPoolSize is empty");
            }
            ConnectionPoolSize = Convert.ToInt32(connectionPoolSizeSection);
        }

        public RedisShardInfo(BKRedisDataType type, IConfigurationSection section)
        {

            ShardDataType = type;

            ShardSize = Convert.ToInt32(section[$"ShardSize"]);

            var bindServerTypesSection = section.GetSection($"BindServerTypes");
            if (bindServerTypesSection is null)
            {
                throw new InvalidOperationException($"bindServerTypes is empty");
            }

            foreach (var bindServerType in bindServerTypesSection.GetChildren())
            {
                if (bindServerType is null)
                {
                    throw new Exception($"bindServerType is empty");
                }

                if (Enum.TryParse<ServerMode>(bindServerType.Value, out var serverMode) == false)
                {
                    throw new InvalidDataException($"bindServerType is invalud, value: {bindServerType.Value}");
                }

                BindServerTypes.Add(serverMode);
            }

            var serviceTypesSection = section.GetSection($"RedisServiceTypes");
            if (serviceTypesSection is null)
            {
                throw new InvalidDataException($"serviceTypes is empty");
            }

            foreach (var serviceType in serviceTypesSection.GetChildren())
            {
                if (Enum.TryParse<RedisServiceType>(serviceType.Value, out var redisServiceType) == false)
                {
                    throw new InvalidDataException($"redisServiceType is invalid, value: {serviceType.Value}");
                }

                ServiceTypes.Add(redisServiceType);
            }

            var builder = ImmutableList.CreateBuilder<RedisConnectionInfo>();
            foreach (var conn in section.GetSection($"Connections").GetChildren())
            {
                var newConn = new RedisConnectionInfo(conn);
                string? hosts = string.Empty;
                newConn.Hosts.ForEach(host => hosts += host);
                CoreLog.Critical.LogFatal(hosts!);
                builder.Add(newConn!);
                
            }
            Connections = builder.ToImmutable();

            var connectionPoolSizeSection = section[$"ConnectionPoolSize"];
            if (connectionPoolSizeSection is null)
            {
                throw new InvalidDataException($"ConnectionPoolSize is empty");
            }
            ConnectionPoolSize = Convert.ToInt32(connectionPoolSizeSection);
        }
    }

    public sealed class RedisConnectionInfo
    {
        
        public ImmutableList<string> Hosts;
        public readonly int ID;//NOTE 센티넬 네임과 일반 서비스 네임은 공존할 수 없음
        public readonly string SentinelName = string.Empty;
        public readonly string Password = string.Empty;
        public readonly int ConnectionTimeout;
        public readonly int ShardMin;
        public readonly int ShardMax;
        

        public RedisConnectionInfo(IConfigurationSection section)
        {
            ID = Convert.ToInt32(section["ID"]);
            SentinelName = section["SentinelName"] ?? String.Empty;            
            ConnectionTimeout = Convert.ToInt32(section["ConnectionTimeout"]);
            ShardMin = Convert.ToInt32(section["ShardMin"]);
            ShardMax = Convert.ToInt32(section["ShardMax"]);
            Password = section["Password"] ?? String.Empty;
            var builder = ImmutableList.CreateBuilder<string>();
            var hosts = section.GetSection("Host")?.GetChildren()?.Select(x => x.Value).ToList();
            if (hosts != null)
            {
                foreach (var item in hosts)
                {
                    if (item != null)
                    {
                        builder.Add(item);
                    }
                }
            }
            Hosts = builder.ToImmutable();
        }
    }
}
