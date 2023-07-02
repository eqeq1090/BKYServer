using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;
using System.Data;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;

namespace BKServerBase.Config.Storage
{
    public sealed class DBConnectionConfig
    {
        private readonly int MinConnectionPoolSize = 30;
        private readonly int MaxConnectionPoolSize = 3000;
        private readonly int ConnectionTimeOut = 15;

        public ImmutableDictionary<BKSchemaType, DBShardInfo> Shards;
        public readonly IsolationLevel IsolationLevel;
        public readonly int DbSessionPoolingSize = 1;

        public DBConnectionConfig(IConfigurationRoot configRoot)
        {
            var builder = ImmutableDictionary.CreateBuilder<BKSchemaType, DBShardInfo>();
            Enum.TryParse(configRoot["Isolation"], false, out IsolationLevel);
            foreach (var item in configRoot.GetSection("Database").GetChildren())
            {
                var newShardInfo = new DBShardInfo(item, MinConnectionPoolSize, MaxConnectionPoolSize, ConnectionTimeOut);
                builder.Add(newShardInfo.ShardDataType, newShardInfo);
            }
            Shards = builder.ToImmutable();
        }

        public string GetShardConnectionString(BKSchemaType type, int shardNum)
        {
            if (Shards.TryGetValue(type, out var shardInfo) ==  false)
            {
                CoreLog.Critical.LogError($"shardInfo is empty, type: {type}, shardNum: {shardNum}");
                return String.Empty;
            }
            if (shardNum  > shardInfo.ShardSize || shardNum < 0)
            {
                CoreLog.Critical.LogError($"shardNum is invalid, type: {type}, shardNum: {shardNum}");
                return String.Empty;
            }
            var target = shardInfo.Connections.Where(x => shardNum <= x.ShardMax && shardNum >= x.ShardMin).FirstOrDefault();
            if (target == null)
            {
                CoreLog.Critical.LogError($"shardInfo connection is empty, type: {type}, shardNum: {shardNum}");
                return String.Empty;
            }
            return target.GetConnectionString();
        }

        public DBShardInfo? GetDBShardInfos(BKSchemaType type)
        {
            if (Shards.TryGetValue(type, out var shardInfo) == false)
            {
                return null;
            }
            return shardInfo;
        }
    }

    public sealed class DBShardInfo
    {
        public readonly BKSchemaType ShardDataType;
        public IsolationLevel IsolationLevel;
        public readonly int ShardSize;
        public readonly bool Sharded;
        public ImmutableList<DBConnectionInfo> Connections;

        public DBShardInfo(IConfigurationSection section, int minPoolSize, int maxPoolSize, int connectionTimeOut)
        {
            Enum.TryParse<BKSchemaType>(section["DBName"], false, out ShardDataType);
            Sharded = Convert.ToBoolean(section["Sharded"]);
            ShardSize = Convert.ToInt32(section["ShardSize"]);

            var builder = ImmutableList.CreateBuilder<DBConnectionInfo>();
            foreach (var conn in section.GetSection("Connections").GetChildren())
            {
                var newConn = new DBConnectionInfo($"{ShardDataType}",conn, minPoolSize, maxPoolSize, connectionTimeOut);
                builder.Add(newConn);
            }
            Connections = builder.ToImmutable();
        }
    }

    public sealed class DBConnectionInfo
    {
        private readonly int m_MaxConnPoolSize;
        private readonly int m_MinConnPoolSize;
        private readonly int m_ConnectionTimeOut;

        public readonly string ActualDBName;
        public readonly string User;
        public readonly string Password;
        public readonly string Host;
        public readonly int Port;
        public readonly int ShardMin;
        public readonly int ShardMax;

        public DBConnectionInfo(string dbname, IConfigurationSection section, int minPoolSize, int maxPoolSize, int connectionTimeOut)
        {
            ActualDBName = $"{dbname}";
            User = section["User"] ?? String.Empty;
            Password = section["Password"] ?? String.Empty;
            Host = section["Host"] ?? String.Empty;
            Port = Convert.ToInt32(section["Port"]);
            ShardMin = Convert.ToInt32(section["ShardMin"]);
            ShardMax = Convert.ToInt32(section["ShardMax"]);

            m_MinConnPoolSize = minPoolSize;
            m_MaxConnPoolSize = maxPoolSize;
            m_ConnectionTimeOut = connectionTimeOut;
        }

        public string GetConnectionString()
        {
            //TODO ConnectionPool 관련 설정은 추가가 필요함.
            return $"Server={Host};Port={Port};UserID={User};Password={Password};Database={ActualDBName};" +
                $"SSL Mode=None;MinimumPoolSize={m_MinConnPoolSize};MaximumPoolSize={m_MaxConnPoolSize};" +
                $"Connection Timeout={m_ConnectionTimeOut};";
        }
    }
}
