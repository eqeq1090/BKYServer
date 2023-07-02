using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Config.Storage;
using BKServerBase.Config;
using BKCommonComponent.Redis.Detail;
using BKServerBase.ConstEnum;
using StackExchange.Redis;
using BKProtocol;
using BKNetwork.Serialize;
using BKServerBase.Logger;

namespace BKCommonComponent.Redis
{
    public sealed class RedisConnectorPool
    {
        private readonly ConcurrentBag<RedisConnector> m_ConnectorPool = new ConcurrentBag<RedisConnector>();
        private readonly ConcurrentQueue<PendingRequestInfo<IRedisConnector>> m_PendingQueue = new();
        private readonly RedisConnector m_StandaloneConnector;
        private readonly int m_ConnectionPoolSize;

        public RedisConnectorPool(BKRedisDataType redisDataType, RedisShardInfo redisShardInfo, RedisComponent redisComponent)
        {
            if (redisShardInfo.Connections.Count is 0)
            {
                throw new Exception($"RedisShardInfo's connections is empty");
            }

            m_ConnectionPoolSize = redisShardInfo.ConnectionPoolSize;

            for (int i = 0; i < redisShardInfo.ConnectionPoolSize; ++i)
            {
                var redisClientName = ConfigManager.Instance.RedisConnectionConf!.GetRedisClientName(i, redisDataType);

                foreach (var redisConnectionInfo in redisShardInfo.Connections)
                {
                    var redisConenctor = new RedisConnector(
                        clientName: redisClientName,
                        connectionInfo: redisConnectionInfo,
                        dataType: redisDataType, 
                        pool: this,
                        redisComponent: redisComponent,
                        isStandalone: false);
                    m_ConnectorPool.Add(redisConenctor);
                }
            }

            {
                var redisClientName = ConfigManager.Instance.RedisConnectionConf!.GetRedisClientName(0, redisDataType);
                m_StandaloneConnector = new RedisConnector(
                    clientName: redisClientName,
                    connectionInfo: redisShardInfo.Connections.First(),
                    dataType: redisDataType, 
                    pool: this,
                    redisComponent: redisComponent, 
                    isStandalone: true);
            }
        }

        public void ConnectToRedisServer()
        {
            foreach (var connector in m_ConnectorPool)
            {
                connector.Connect();
            }

            m_StandaloneConnector.Connect();
        }

        public Task<IRedisConnector> GetClientAsync()
        {
            if (m_ConnectionPoolSize is 0)
            {
                throw new Exception($"GetClientAsync failed, Invalid usage, connectionPoolSize: 0");
            }

            if (m_ConnectorPool.TryTake(out var connector) is false)
            {
                var tcs = new TaskCompletionSource<IRedisConnector>();
                m_PendingQueue.Enqueue(new PendingRequestInfo<IRedisConnector>(tcs));
                return tcs.Task;
            }

            return Task.FromResult((IRedisConnector)connector);
        }

        public RedisConnector GetStandaloneConnector()
        {
            return m_StandaloneConnector;
        }

        public void Subscribe(RedisChannel channel, out ChannelMessageQueue channelMsgQueue)
        {
            m_StandaloneConnector.Subscribe(channel, out channelMsgQueue);
        }
        
        public void Return(RedisConnector connector)
        {
            if (m_PendingQueue.TryDequeue(out var requestInfo))
            {
                requestInfo.Resolove(connector);
                return;
            }

            m_ConnectorPool.Add(connector);
        }
    }
}
