using BKServerBase.Threading;
using BKServerBase.Component;
using BKNetwork.Dispatch;
using BKProtocol;
using BKGameServerComponent.Actor;
using BKGameServerComponent.Session;
using BKServerBase.Logger;
using BKNetwork.Listener;
using BKNetwork.API;
using BKGameServerComponent.Channel;
using BKNetwork.Discovery;
using BKServerBase.Config;
using BKCommonComponent.Detail;
using BKServerBase.ConstEnum;
using BKCommonComponent.Redis;
using System.Collections.Concurrent;
using BKNetwork.ConstEnum;
using BKNetwork.Interface;
using BKServerBase.Interface;
using BKServerBase.Messaging.Detail;
using BKServerBase.Messaging;
using BKNetwork.Dispatch.Manager;
using BKServerBase.Util;
using BKDataLoader.MasterData;
using Elasticsearch.Net;

namespace BKGameServerComponent
{
    internal delegate void OnPlayerDispatchEventHandler<T>(Player sender, T msg)
        where T : IMsg, new();

    internal delegate void OnClientSessionDispatchEventHandler<T>(SessionContext ctx, T msg)
        where T : IMsg, new();

    internal delegate void OnMatchSessionDispatchEventHandler<T>(MatchSessionContext ctx, T msg)
        where T : IMsg, new();

    internal delegate void OnTeamSessionDispatchEventHandler<T>(TeamSessionContext ctx, T msg)
        where T : IMsg, new();

    public partial class GameServerComponent : BaseSingleton<GameServerComponent>, IComponent, IJobActor<GameServerComponent>
    {
        private readonly EventTimer m_EventTimer;
        private readonly ICommandExecutor m_CommandExecutor;
        private readonly SessionManager m_ClientSessionManager;
        private readonly SessionManager m_MatchSessionManager;
        private readonly SessionManager m_GlobalSessionManager;
        private readonly ChannelManager m_ChannelManager;
        
        private readonly ServiceDiscoveryManager m_ServiceDisconveryManager;

        private readonly GameNetworkListener m_ClientListener;
        private readonly ServerDispatchManager m_ClientDispatcher = new ServerDispatchManager();
        
        private readonly GameNetworkListener m_MatchServiceListener;
        private readonly ServerDispatchManager m_MatchServiceDispatcher = new ServerDispatchManager();

        private readonly GameNetworkListener m_GlobalServiceListener;
        private readonly ServerDispatchManager m_GlobalServiceDispatcher = new ServerDispatchManager();
        private readonly JobDispatcher m_JobDispatcher;

        private ThreadCoordinator m_ThreadCoordinator;

        private ConcurrentDictionary<PlayerUID, SessionID> m_PlayerSessionDict = new();

        private GameServerComponent()
        {
            m_ThreadCoordinator = new ThreadCoordinator(ConfigManager.Instance.CommonConfig.FPS);
            m_ThreadCoordinator.Initialize();

            m_JobDispatcher = new JobDispatcher(false);

            m_ClientListener = new GameNetworkListener(
                ConfigManager.Instance.GameServerConf!.FromClientPort,
                m_ClientDispatcher,
                OnClientSessionAdd,
                OnClientSessionRemove,
                OnClientSessionError
            );

            m_MatchServiceListener = new GameNetworkListener(
                ConfigManager.Instance.GameServerConf!.FromMatchPort,
                m_MatchServiceDispatcher,
                OnMatchSessionAdd,
                OnMatchSessionRemove,
                OnMatchSessionError
            );

            m_GlobalServiceListener = new GameNetworkListener(
                ConfigManager.Instance.GameServerConf!.FromGlobalPort,
                m_GlobalServiceDispatcher,
                OnGlobalSessionAdd,
                OnGlobalSessionRemove,
                OnGlobalSessionError
            );

            m_EventTimer = new EventTimer();
            m_CommandExecutor = CommandExecutor.CreateCommandExecutor(nameof(GameServerComponent), 0);

            var connectionInfo = MakeServerDiscoveryConnectionInfo();

            m_ServiceDisconveryManager = new ServiceDiscoveryManager(
                connectionInfo,
                new()
                {
                    { ServiceDiscoveryInfoType.g2a, true },
                });
            
            m_ClientSessionManager = new SessionManager(m_ThreadCoordinator);

            m_MatchSessionManager = new SessionManager(m_ThreadCoordinator);

            m_GlobalSessionManager = new SessionManager(m_ThreadCoordinator);

            m_ChannelManager = new ChannelManager(m_ThreadCoordinator);
        }

        private IRedisComponent RedisComponent => ComponentManager.Instance.GetComponent<IRedisComponent>()
            ?? throw new Exception($"RedisComponent is not registerd in Component");
        public GameServerComponent Owner => this;
        public string QuantumCodeVersion { get; private set; } = string.Empty;

        public (bool success, OnComponentInitializedHandler? InitDoneFunc) Initialize()
        {
            LoadData();
            
            RegisterDispatcher();
            
            RegisterPktMetricDispatcher();
            
            RegisterNetworkTraceLogDispatcher();
            
            m_ClientSessionManager.Initialize();
            m_ChannelManager.Initialize();

            m_MatchSessionManager.Initialize();

            m_GlobalSessionManager.Initialize();

            m_ClientListener.Initialize();
            m_MatchServiceListener.Initialize();
            m_GlobalServiceListener.Initialize();

            var apiDispatchComponent = ComponentManager.Instance.GetComponent<APIDispatchComponent>();
            if (apiDispatchComponent != null)
            {
                apiDispatchComponent.SetServiceDiscovery(m_ServiceDisconveryManager);
            }
            return (true, () =>
            {
            });
        }        


        public bool OnUpdate(double delta)
        {
            m_EventTimer.Update();
            m_CommandExecutor.Execute();
            m_JobDispatcher.RunAction();
            return true;
        }

        public void Invoke(Command command)
        {
            m_CommandExecutor.Invoke(command);
        }

        public bool Shutdown()
        {
            return true;
        }

        public void RegisterDispatcher()
        {
            // Client dispatcher
            RegisterPlayerContentsDispatcher();
            //RegisterClientMatchHandler();
            //RegisterShopDispatcher();
            //RegisterSkinDispatcher();
            //RegisterFriendDispatcher();
            //RegisterChatDispatcher();
            //RegisterTeamDispatcher();

            //RegisterSystemPubSubDispatcher();
            //RegisterFriendPubSubDispatcher();
            //RegisterFriendlyRoomPubsubDispatcher();
            //RegisterChatPubSubDispatcher();
            
            //RegisterAdminProtocol();
            //RegisterRewardDispatcher();

            //// Server dispatcher
            //RegisterMatchServerDispatcher();
            //RegisterGlobalServerDispatcher();
        }

        public void RegisterPktMetricDispatcher()
        {

        }

        public void RegisterNetworkTraceLogDispatcher()
        {

        }

        public void LoadData()
        {

        }

        private void RegisterPlayerDispatcher<T>(OnPlayerDispatchEventHandler<T> handler, OnPreDispatchEventHandler? preDispatchEventHandler = null, bool passFlag = false)
            where T : IMsg, new()
        {
            m_ClientDispatcher!.RegisterDispatcher<T>((msg, ctx, passFlag) =>
            {
                if (ctx is not SessionContext sessionContext)
                {
                    LogExtensions.LogError(GameNetworkLog.Critical, $"context is not sessionContext");
                    return;
                }

                if (passFlag)
                {
                    return;
                }

                if (sessionContext.Player is null)
                {
                    LogExtensions.LogError(GameNetworkLog.Critical, $"SessionContext's player is null");
                    return;
                }

                sessionContext.Player.PostHandler<T>(handler, msg);
                return;
            });
        }


        private void RegisterClientSessionDispatcher<T>(OnClientSessionDispatchEventHandler<T> handler, OnPreDispatchEventHandler? preDispatchEventHandler = null)
            where T : IMsg, new()
        {
            m_ClientDispatcher!.RegisterDispatcher<T>((msg, ctx, passFlag) =>
            {
                if (ctx is not SessionContext sessionContext)
                {
                    LogExtensions.LogError(GameNetworkLog.Critical, $"context is not sessionContext");
                    return;
                }

                if (passFlag)
                {
                    //ERROR
                    return;
                }

                sessionContext.PostHandler<T>(handler, msg);
            });
        }

        private void RegisterMatchDispatcher<T>(OnMatchSessionDispatchEventHandler<T> handler, OnPreDispatchEventHandler? preDispatchEventHandler = null)
            where T : IMsg, new()
        {
            m_MatchServiceDispatcher!.RegisterDispatcher<T>((msg, ctx, passFlag) =>
            {
                if (ctx is not MatchSessionContext sessionContext)
                {
                    LogExtensions.LogError(GameNetworkLog.Critical, $"context is not MatchSessionContext");
                    return;
                }

                handler(sessionContext, msg);
            });
        }

        private void RegisterGlobalDispatcher<T>(OnTeamSessionDispatchEventHandler<T> handler, OnPreDispatchEventHandler? preDispatchEventHandler = null)
           where T : IMsg, new()
        {
            m_GlobalServiceDispatcher!.RegisterDispatcher<T>((msg, ctx, passFlag) =>
            {
                if (ctx is not TeamSessionContext sessionContext)
                {
                    LogExtensions.LogError(GameNetworkLog.Critical, $"context is not MatchSessionContext");
                    return;
                }

                handler(sessionContext, msg);
            });
        }

        private void OnUpdateTimer(object? state)
        {
            // m_ChannelManager.PostUpdateRedisExpiry();
        }

        private List<ServiceDiscoveryManager.ConnectionInfo> MakeServerDiscoveryConnectionInfo()
        {
            var result = new List<ServiceDiscoveryManager.ConnectionInfo>();
            {
                string ipAddress = string.Empty;
                int port = 0;
                if (ConfigManager.Instance.GameServerConf!.IsValidPublicIp())
                {
                    ipAddress = ConfigManager.Instance.GameServerConf!.PublicIp;
                    port = ConfigManager.Instance.GameServerConf!.PublicPort;
                }
                else
                {
                    ipAddress = m_ClientListener.IpAddress;
                    port = m_ClientListener.Port;
                }

                if (string.IsNullOrEmpty(ipAddress))
                {
                    throw new Exception($"client ipAddress is empty");
                }

                var connectionInfo = new ServiceDiscoveryManager.ConnectionInfo()
                {
                    InfoType = BKNetwork.ConstEnum.ServiceDiscoveryInfoType.c2g,
                    IpAddress = ipAddress,
                    Port = port,
                };
                result.Add(connectionInfo);
            }

            {
                string ipAddress = string.Empty;
                int matchPort = 0;
                if (ConfigManager.Instance.GameServerConf!.UseDocker)
                {
                    ipAddress = ConfigManager.Instance.GameServerConf!.DockerIp;
                    matchPort = ConfigManager.Instance.GameServerConf!.FromMatchPort;
                }
                else
                {
                    ipAddress = m_MatchServiceListener.IpAddress;
                    matchPort = m_MatchServiceListener.Port;
                }
                var connectionInfo = new ServiceDiscoveryManager.ConnectionInfo()
                {
                    InfoType = BKNetwork.ConstEnum.ServiceDiscoveryInfoType.m2g,
                    IpAddress = ipAddress,
                    Port = matchPort
                };
                result.Add(connectionInfo);
            }

            {
                string ipAddress = string.Empty;
                int globalPort = 0;
                if (ConfigManager.Instance.GameServerConf!.UseDocker)
                {
                    ipAddress = ConfigManager.Instance.GameServerConf!.DockerIp;
                    globalPort = ConfigManager.Instance.GameServerConf!.FromGlobalPort;
                }
                else
                {
                    ipAddress = m_GlobalServiceListener.IpAddress;
                    globalPort = m_GlobalServiceListener.Port;
                }
                var connectionInfo = new ServiceDiscoveryManager.ConnectionInfo()
                {
                    InfoType = BKNetwork.ConstEnum.ServiceDiscoveryInfoType.gl2g,
                    IpAddress = ipAddress,
                    Port = globalPort
                };
                result.Add(connectionInfo);
            }
            return result;
        }

        public JobDispatcher GetDispatcher()
        {
            return m_JobDispatcher;
        }

        public Future<int> PostCreateTimerEvent(OnTimerHandler action, long duration, bool isRepeat, IActor taskOwner)
        {
            return this.PostFuture(self =>
            {
                return m_EventTimer.CreateTimerEvent(action, duration, isRepeat);
            }, taskOwner);
        }

        public void PostCancelTimerEvent(int timerID)
        {
            this.Post(self =>
            {
                m_EventTimer.RemoveTimerEvent(timerID);
            });
        }
    }
}
