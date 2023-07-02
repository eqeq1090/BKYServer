using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Config;
using BKServerBase.Config.Storage;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKCommonComponent.Redis.Detail;
using BKNetwork.Serialize;
using BKProtocol;

namespace BKCommonComponent.Redis
{
    public interface IRedisConnector : IDisposable
    {
        IDatabase GetDatabase(int db = 0);
    }

    public interface IRedisPusbsubConnector
    {
        Task<bool> Publish(IPubsubMsg message);
        bool Subscribe();
        bool Subscribe(string key);
    }

    public sealed class RedisConnector : IRedisConnector, IRedisPusbsubConnector
    {
        private readonly RedisConnectorPool m_Pool;
        private readonly RedisComponent m_RedisComponent;
        private readonly ConfigurationOptions m_Config;
        private readonly BKRedisDataType m_DataType;
        private IConnectionMultiplexer? m_Connection;
        private bool m_IsStandalone;

        public RedisConnector(
            string clientName, 
            RedisConnectionInfo connectionInfo, 
            BKRedisDataType dataType, 
            RedisConnectorPool pool, 
            RedisComponent redisComponent, 
            bool isStandalone)
        {
            m_Pool = pool;
            m_RedisComponent = redisComponent;
            m_IsStandalone = isStandalone;
            m_DataType = dataType;

            m_Config = new ConfigurationOptions();
            m_Config.ConnectTimeout = connectionInfo.ConnectionTimeout;
            m_Config.AbortOnConnectFail = true;
            m_Config.ConnectRetry = 3;
            m_Config.SyncTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
            m_Config.AsyncTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
            m_Config.KeepAlive = 30;
            m_Config.ClientName = clientName;
            m_Config.DefaultDatabase = 0;

            if (string.IsNullOrEmpty(connectionInfo.SentinelName) is false)
            {
                m_Config.ServiceName = connectionInfo.SentinelName;
            }

            if (string.IsNullOrEmpty(connectionInfo.Password) is false)
            {
                m_Config.Password = connectionInfo.Password;
            }

            foreach (var host in connectionInfo.Hosts)
            {
                m_Config.EndPoints.Add(host);
            }
        }

        public void Connect()
        {
            m_Connection = ConnectionMultiplexer.Connect(m_Config);
            m_Connection.InternalError += OnInternalError;
            m_Connection.ConnectionFailed += OnConnectionFailed;
            m_Connection.ConnectionRestored += OnConnectionRestored;
            m_Connection.ErrorMessage += OnErrorMessage;
        }

        public IDatabase GetDatabase(int db = 0)
        {
            if (m_Connection!.IsConnected is false)
            {
                Reconnect();
            }

            return m_Connection!.GetDatabase(db);
        }

        public async Task<bool> Publish(IPubsubMsg message)
        {
            try
            {
                var convertMessage = PubsubMsgPairGenerator.Instance.Serialize(message);
                if (convertMessage is null)
                {
                    CoreLog.Normal.LogError($"PubsubMsg serialize failed, message: {message.MsgType}");
                    return false;
                }

                if (m_Connection!.IsConnected is false)
                {
                    Reconnect();
                }

                var channel = RedisKeyGroup.MakeChannelName(m_DataType);
                await m_Connection!
                    .GetSubscriber()
                    .PublishAsync(channel, convertMessage, CommandFlags.None);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"Redis publish failed, exception: {e}");
                return false;
            }
        }

        public void Subscribe(RedisChannel channel, out ChannelMessageQueue channelMsgQueue)
        {
            if (m_Connection!.IsConnected is false)
            {
                Reconnect();
            }

            channelMsgQueue = m_Connection!
                .GetSubscriber()
                .Subscribe(channel, CommandFlags.None);
        }

        public bool Subscribe()
        {
            var channel = RedisKeyGroup.MakeChannelName(m_DataType);
            Subscribe(channel, out var channelMsgQueue);

            var result = m_RedisComponent.RegisterChannelMsgQueue(channel, channelMsgQueue);
            if (result is false)
            {
                channelMsgQueue.Unsubscribe();
                return false;
            }

            return true;
        }

        public bool Subscribe(string key)
        {
            var channel = RedisKeyGroup.MakeChannelName(m_DataType, key);
            Subscribe(channel, out var channelMsgQueue);

            var result = m_RedisComponent.RegisterChannelMsgQueue(channel, channelMsgQueue);
            if (result is false)
            {
                channelMsgQueue.Unsubscribe();
                return false;
            }

            return true;
        }
        
        public IConnectionMultiplexer Connection => m_Connection 
            ?? throw new Exception($"RedisConnector is not connected");

        public void Dispose()
        {
            if (m_IsStandalone)
            {
                return;
            }

            m_Pool.Return(this);
        }


        private void OnErrorMessage(object? sender, RedisErrorEventArgs e)
        {
            CoreLog.Critical.LogError($"redis error message: {e.Message} from redisServer, endPoint: {e.EndPoint}");
        }

        private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
        {
            CoreLog.Critical.LogError($"redisClient is reconnected, endPoint: {e.EndPoint}");
        }

        private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            CoreLog.Critical.LogError($"reisClient failed to connect, exception: {e.Exception}, endPoint: {e.EndPoint}");
        }

        private void OnInternalError(object? sender, InternalErrorEventArgs e)
        {
            CoreLog.Critical.LogError($"redisServer internalError, exception: {e.Exception}, endPoint: {e.EndPoint}");
        }

        private void Close()
        {
            m_Connection!.InternalError -= OnInternalError;
            m_Connection!.ConnectionFailed -= OnConnectionFailed;
            m_Connection!.ConnectionRestored -= OnConnectionRestored;
            m_Connection!.ErrorMessage -= OnErrorMessage;
            m_Connection!.Close();
        }

        private void Reconnect()
        {
            CoreLog.Critical.LogError($"RedisClient is disconnected for {m_Config.ClientName}");
            Close();

            CoreLog.Normal.LogInfo($"Connecting to redisServer for {m_Config.ClientName}");
            Connect();
        }
    }
}
