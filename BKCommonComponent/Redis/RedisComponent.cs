using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using BKServerBase.Component;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKCommonComponent.Detail;
using BKCommonComponent.Redis.Detail;
using BKNetwork.Dispatch;
using BKNetwork.Redis.Dispatch;
using BKNetwork.Serialize;
using BKProtocol;
using BKProtocol.Enum;
using BKProtocol.Pubsub;
using static Swan.Terminal;

namespace BKCommonComponent.Redis
{
    public interface IRedisComponent : IComponent
    {
        TimeSpan ExpiryTimeSpan { get; }

        Task<IRedisConnector> GetClientAsync(BKRedisDataType redisDataType);
        IRedisPusbsubConnector GetPubsubClient(BKRedisDataType redisDataType);
        bool Unsubscribe(BKRedisDataType redisDataType, string key);
        bool Unsubscribe(BKRedisDataType redisDataType);
        void RegisterPubsubDispatcher<T>(OnRedisMsgDispatchHandler<T> handler, OnPreDispatchEventHandler? preDispatchEventHandler = null)
            where T : IPubsubMsg, new();

        void UpdateSessionDataExpiry(IEnumerable<(long playerUID, string sessionID)> playerUIDs);
        void UpdatePresenceDataExpiry(IEnumerable<PresenceData> playerUIDs);
    }

    public sealed class RedisComponent : IRedisComponent
    {
        private readonly TimeSpan m_ExpiryTimeSpan = TimeSpan.FromSeconds(CommonConsts.RedisExpiryUpdateSeconds * 3);
        private readonly Dictionary<BKRedisDataType, RedisConnectorPool> m_ConnectionPool = new Dictionary<BKRedisDataType, RedisConnectorPool>();
        private readonly ConcurrentQueue<IPubsubMsg> m_PubsubMsgQueue = new ConcurrentQueue<IPubsubMsg>();
        private readonly Dictionary<RedisChannel, ChannelMessageQueue> m_ChannelMsgQueueMap = new Dictionary<RedisChannel, ChannelMessageQueue>();
        private readonly RedisMsgDispatchManager m_RedisMsgDispatcherManager = new RedisMsgDispatchManager();

        public RedisComponent()
        {
        }

        public TimeSpan ExpiryTimeSpan => m_ExpiryTimeSpan;

        public (bool success, OnComponentInitializedHandler? InitDoneFunc) Initialize()
        {
            PubsubMsgPairGenerator.Instance.Initialize();

            return (true, () =>
            {
                if (ConfigManager.Instance.RedisConnectionConf is null)
                {
                    return;
                }

                SetUpConnectionPool();
                SetUpSubscribes();
            }
            );
        }

        public async Task<IRedisConnector> GetClientAsync(BKRedisDataType redisDataType)
        {
            if (m_ConnectionPool.TryGetValue(redisDataType, out var pool) is false)
            {
                throw new Exception($"not supported redisDataType: {redisDataType}");
            }

            return await pool.GetClientAsync();
        }

        public IRedisPusbsubConnector GetPubsubClient(BKRedisDataType redisDataType)
        {
            if (m_ConnectionPool.TryGetValue(redisDataType, out var pool) is false)
            {
                throw new Exception($"not supported redisDataType: {redisDataType}");
            }

            return pool.GetStandaloneConnector();
        }

        public bool Unsubscribe(BKRedisDataType redisDataType)
        {
            var channel = RedisKeyGroup.MakeChannelName(redisDataType);
            if (m_ChannelMsgQueueMap.ContainsKey(channel) is false)
            {
                return false;
            }

            m_ChannelMsgQueueMap[channel].Unsubscribe(CommandFlags.None);
            return true;
        }

        public bool Unsubscribe(BKRedisDataType redisDataType, string key)
        {
            var channel = RedisKeyGroup.MakeChannelName(redisDataType, key);
            if (m_ChannelMsgQueueMap.ContainsKey(channel) is false)
            {
                return false;
            }

            m_ChannelMsgQueueMap[channel].Unsubscribe(CommandFlags.None);
            return true;
        }

        public bool OnUpdate(double delta)
        {
            if (m_PubsubMsgQueue.TryDequeue(out var msg) is false)
            {
                return true;
            }

            m_RedisMsgDispatcherManager.Dispatch(msg);
            return true;
        }

        public bool Shutdown()
        {
            return true;
        }

        public void RegisterPubsubDispatcher<T>(OnRedisMsgDispatchHandler<T> handler, OnPreDispatchEventHandler? preDispatchEventHandler = null)
            where T : IPubsubMsg, new()
        {
            m_RedisMsgDispatcherManager!.RegisterDispatcher<T>((msg) =>
            {
                handler(msg);
            });
        }

        public bool RegisterChannelMsgQueue(RedisChannel channel, ChannelMessageQueue channelMsgQueue)
        {
            if (m_ChannelMsgQueueMap.ContainsKey(channel))
            {
                CoreLog.Critical.LogError($"RegisterChannelMsgQueue failed, duplicated channl name: {channel}");
                return false;
            }

            channelMsgQueue.OnMessage(OnPubsubMessage);
            m_ChannelMsgQueueMap.Add(channel, channelMsgQueue);

            return true;
        }

        public void UpdateSessionDataExpiry(IEnumerable<(long playerUID, string sessionID)> sessionDatas)
        {
            if (m_ConnectionPool.TryGetValue(BKRedisDataType.session, out var pool) is false)
            {
                throw new Exception($"not supported redisDataType: {BKRedisDataType.session}");
            }

            using var redisConnector = pool.GetStandaloneConnector();
            var db = redisConnector.GetDatabase(0);

            var redisOperator = new RedisOperator();
            foreach (var sessionData in sessionDatas)
            {
                var key = RedisKeyGroup.MakeSessionKey(sessionData.playerUID);
                redisOperator.AddStringAsync(db, key, sessionData.sessionID, expiry: m_ExpiryTimeSpan, flags: CommandFlags.FireAndForget)
                    .ContinueWith(_ => { });
            }
        }

        public void UpdatePresenceDataExpiry(IEnumerable<PresenceData> presenceDatas)
        {
            if (m_ConnectionPool.TryGetValue(BKRedisDataType.presence, out var pool) is false)
            {
                throw new Exception($"not supported redisDataType: {BKRedisDataType.presence}");
            }

            using var redisConnector = pool.GetStandaloneConnector();
            var db = redisConnector.GetDatabase(0);

            var redisOperator = new RedisOperator();
            foreach (var presenceData in presenceDatas)
            {
                var key = RedisKeyGroup.MakePresenceKey(presenceData.PlayerUID);
                redisOperator.AddAsync(db, key, presenceData, expiry: m_ExpiryTimeSpan, flags: CommandFlags.FireAndForget)
                    .ContinueWith(_ => { });
            }
        }

        private void SetUpConnectionPool()
        {
            var serverMode = ConfigManager.Instance.ServerMode;
            foreach (var (redisDataType, redisShardInfo) in ConfigManager.Instance.RedisConnectionConf!.Shards)
            {
                if (serverMode is not ServerMode.AllInOne &&
                    redisShardInfo.BindServerTypes.Contains(serverMode) is false) 
                {
                    continue;
                }
                                
                var hosts = redisShardInfo.Connections.SelectMany(e => e.Hosts);
                var joinedHosts = String.Join(',', hosts);

                CoreLog.Normal.LogInfo($"Connecting to redis, redisDataType: {redisDataType}, hosts: {joinedHosts}");
                var redisConnectorPool = new RedisConnectorPool(redisDataType, redisShardInfo, this);
                m_ConnectionPool.Add(redisDataType, redisConnectorPool);
            }
            
            foreach (var pool in m_ConnectionPool.Values)
            {
                pool.ConnectToRedisServer();
            }
        }

        private void SetUpSubscribes()
        {
            var serverMode = ConfigManager.Instance.ServerMode;
            foreach (var (redisDataType, redisShardInfo) in ConfigManager.Instance.RedisConnectionConf!.Shards)
            {
                if (serverMode is not ServerMode.AllInOne &&
                    redisShardInfo.BindServerTypes.Contains(serverMode) is false)
                {
                    continue;
                }

                if (redisShardInfo.ServiceTypes.Contains(RedisServiceType.Pubsub))
                {
                    RegisterSubscribe(redisDataType);
                }
            }
        }

        private void RegisterSubscribe(BKRedisDataType redisDataType)
        {
            if (m_ConnectionPool.ContainsKey(redisDataType) is false)
            {
                throw new Exception($"Subscribe is not supported, redisDataType: {redisDataType}");
            }

            var channel = RedisKeyGroup.MakeChannelName(redisDataType);

            CoreLog.Normal.LogInfo($"Subscribe for redis, channelName: {channel}, redisDataType: {redisDataType}");

            m_ConnectionPool[redisDataType].Subscribe(channel, out var channelMsgQueue);
            var result = RegisterChannelMsgQueue(channel, channelMsgQueue);
            if (result is false)
            {
                throw new Exception($"RegisterSubscribe failed, channelName: {channel}, redisDataType: {redisDataType}");
            }
        }

        private void OnPubsubMessage(ChannelMessage channelMsg)
        {
            var message = channelMsg.Message.ToString();
            var jsonObj = JObject.Parse(message);
            if (jsonObj == null)
            {
                CoreLog.Critical.LogError($"OnPubsubMessage json parsing failed, message: {message}");
                return;
            }

            var msgTypeValue = (PubsubMsgType)jsonObj.Value<int>("MsgType");
            if (msgTypeValue == PubsubMsgType.Invalid)
            {
                CoreLog.Critical.LogError($"OnPubsubMessage msgType is invalid");
                return;
            }

            var parsedMsg = PubsubMsgPairGenerator.Instance.Deserialize(msgTypeValue, message);
            if (parsedMsg is null)
            {
                CoreLog.Critical.LogError($"OnPubsubMessage deserialize failed, msgType: {msgTypeValue}, message: {message}");
                return;
            }

            m_PubsubMsgQueue.Enqueue(parsedMsg);
        }
    }
}
