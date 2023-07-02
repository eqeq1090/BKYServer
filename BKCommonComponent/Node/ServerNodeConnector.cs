using System.Diagnostics;
using System.Net;
using System.Xml;
using BKServerBase.Component;
using BKServerBase.Config;
using BKServerBase.Config.Server;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKCommonComponent.Detail;
using BKNetwork.API;
using BKNetwork.ConstEnum;
using BKNetwork.Discovery;
using BKNetwork.Dispatch;
using BKNetwork.Dispatch.Manager;
using BKNetwork.Interface;
using BKProtocol;
using BKProtocol.Enum;
using BKProtocol.S2S;
using static BKNetwork.API.LoadBalancer;

namespace BKCommonComponent.Node
{
    public delegate void OnDispatchEventHandler<T>(IContext ctx, T msg)
        where T : IMsg, new();

    public class ServerNodeConnector : IDisposable
    {
        private const double m_KeepAliveIBroadcastntervalMs = 3000;
        private const double m_KeepAliveDelayMs = 1000;

#if DEBUG
        private const double m_KeepAliveCloseLimitMs = 9999999; // debug test
#else
        private const double m_KeepAliveCloseLimitMs = 10000;
#endif
        private const double m_KeepAliveClosingCheckIntervalMs = 1500;

        private readonly ClientDispatchManager m_ClientDispatchManager = new ClientDispatchManager();
        private readonly Dictionary<ServerMode, List<ServerNode>> m_ServerNodeMap = new Dictionary<ServerMode, List<ServerNode>>();
        private double m_KeepAliveBroadcastAccuDelta = 0;
        private double m_KeepAliveClosingCheckAccuDelta = 0;
        private bool m_IsStarted;
        private IActor m_HandlerActor;
        private ServiceDiscoveryInfoType m_NodeType;

        private EventTimer m_TimerTaskEvent;

        public ServerNodeConnector(IActor actor)
        {
            m_TimerTaskEvent = new EventTimer();
            RegisterClientKeepAliveHandler();
            m_HandlerActor = actor;
        }

        public IReadOnlyList<ServerNode> GetServerNodeList(ServerMode serverMode) => m_ServerNodeMap[serverMode];
        
        public void Broadcast(ServerMode serverMode, IMsg msg)
        {
            if (m_ServerNodeMap.ContainsKey(serverMode) is false)
            {
                throw new Exception($"PickServerNode failed, serverMode is not supported: {serverMode}");
            }

            foreach (var serverNode in m_ServerNodeMap[serverMode])
            {
                var result = serverNode.Send(msg);
                if (result != SendResult.Success)
                {
                    GameNetworkLog.Critical.LogError($"Failed to send packet for {serverNode}({serverNode.RemoteEndPoint}), msg: {msg.msgType}");
                }
            }
        }

        public ServerNode PickServerNode(ServerMode serverMode)
        {
            if (m_ServerNodeMap.ContainsKey(serverMode) is false)
            {
                throw new Exception($"PickServerNode failed, serverMode is not supported: {serverMode}");
            }

            var serverList = m_ServerNodeMap[serverMode];
            if (serverList.Count is 0)
            {
                throw new Exception($"PickServerNode failed, serverList's count is zero: {serverMode}");
            }

            var serverIndex = RandomPicker.Next(serverList.Count);

            var serverNode = serverList[serverIndex];
            return serverNode;
        }

        public bool SendToNode(ServerMode targetServerMode, int serverID, IMsg msg)
        {
            if (m_ServerNodeMap.ContainsKey(ServerMode.GameServer) is false)
            {
                throw new Exception($"PickServerNode failed, serverMode is not supported: {targetServerMode}");
            }

            var serverList = m_ServerNodeMap[targetServerMode];
            if (serverList.Count is 0)
            {
                CoreLog.Critical.LogError($"PickServerNode failed, serverList's count is zero: {targetServerMode}");
                return false;
            }

            var serverNode = serverList
                .Where(e => e.ServerID == serverID)
                .FirstOrDefault();
            if (serverNode is null)
            {
                CoreLog.Critical.LogError($"Can not find serverNode, serverID: {serverID}");
                return false;
            }

            var result = serverNode.Send(msg);
            return result == SendResult.Success;
        }

        public void BroadcastResult<T>(ServerMode serverMode, MsgErrorCode errorCode)
            where T : IResMsg, new()
        {
            var msg = new T();
            msg.errorCode = errorCode;

            Broadcast(serverMode, msg);
        }

        public void BroadcastResult<T>(ServerMode serverMode, MsgErrorCode errorCode, Action<T> setupCallback)
            where T : IResMsg, new()
        {
            var msg = new T();
            msg.errorCode = errorCode;
            setupCallback(msg);

            Broadcast(serverMode, msg);
        }

        private void Start()
        {
            foreach ((ServerMode serverMode, List<ServerNode> serverNodeList) in m_ServerNodeMap)
            {
                foreach (var serverNode in serverNodeList)
                {
                    serverNode.Connect(retry: Timeout.Infinite);
                }
            }

            BroadcastKeepAlive();

            m_IsStarted = true;
        }

        public void WaitInitDone()
        {
            try
            {
                var apiDispatcherComponent = ComponentManager.Instance.GetComponent<APIDispatchComponent>();
                if (apiDispatcherComponent == null || apiDispatcherComponent.ServiceDiscovery == null)
                {
                    CoreLog.Critical.LogError($"WaitInitDone failed, APIDispatchComponent is empty");
                    return;
                }
                IServiceDiscovery? discovery = null;
                switch (ConfigManager.Instance.ServerMode)
                {
                    case ServerMode.AllInOne:
                        {
                            discovery = new LocalServiceDiscovery();
                            break;
                        }
                    case ServerMode.GameServer:
                    case ServerMode.MatchServer:
                    case ServerMode.GlobalServer:
                        {
                            discovery = apiDispatcherComponent.ServiceDiscovery;
                            break;
                        }
                }
                if (discovery == null)
                {
                    CoreLog.Critical.LogError($"WaitInitDone failed, disconvery is null");
                    return;
                }
                var gameServerList = discovery.GetTargetServers(m_NodeType);
                if (gameServerList.Count is 0)
                {
                    CoreLog.Normal.LogInfo($"Waiting to find gameServerList from discovery server");
                    return;
                }
                var endPointMap = new Dictionary<ServerMode, string[]>(capacity: gameServerList.Count)
                {
                    {
                        ServerMode.GameServer,
                        gameServerList.Select(e => e.ToString()).ToArray()
                    },
                };
                LoadServerNode(endPointMap);
                Start();
            }
            finally
            {
                if (m_IsStarted == false)
                {
                    m_TimerTaskEvent.CreateTimerEvent(WaitInitDone, 2000, false);
                }
            }
        }

        public void Initialize(ServiceDiscoveryInfoType nodeType)
        {
            m_NodeType = nodeType;
            WaitInitDone();
        }

        public bool Shutdown()
        {
            return true;
        }

        public bool OnUpdate(double delta)
        {
            m_TimerTaskEvent.Update();
            if (m_IsStarted is false)
            {
                return true;
            }

            m_KeepAliveBroadcastAccuDelta += delta;
            if (m_KeepAliveBroadcastAccuDelta > m_KeepAliveIBroadcastntervalMs)
            {
                BroadcastKeepAlive();
                m_KeepAliveBroadcastAccuDelta = 0;
            }

            m_KeepAliveClosingCheckAccuDelta += delta;
            if (m_KeepAliveClosingCheckAccuDelta > m_KeepAliveClosingCheckIntervalMs)
            {
                CheckServerNodeClosing();
                m_KeepAliveClosingCheckAccuDelta = 0;
            }

            return true;
        }

        public void Dispose()
        {
        }

        private void OnServerNodeCloseByKeepAlive(ServerNode serverNode)
        {
            CoreLog.Critical.LogFatal($"OnServerNodeCloseByKeepAlive: {serverNode.ServerMode}@{serverNode.RemoteEndPoint}");

            if (serverNode.IsConnected)
            {
                return;
            }

            if (serverNode.IsConnecting)
            {
                return;
            }

            BackgroundJob.Execute(() =>
            {
                serverNode.Reconnect();
            });
        }

        private void LoadServerNode(Dictionary<ServerMode, string[]> endPointsMap)
        {
            foreach ((ServerMode serverMode, string[] endPoints) in endPointsMap)
            {
                var serverNodeList = endPoints.Select(endPoint =>
                {
                    if (IPEndPoint.TryParse(endPoint, out IPEndPoint? remoteEndPoint) == false)
                    {
                        throw new InvalidOperationException($"invalid remoteEndPoint: {endPoint}");
                    }

                    var serverNode = new ServerNode(
                        serverMode,
                        remoteEndPoint,
                        m_ClientDispatchManager);

                    return serverNode;
                }).ToList();

                m_ServerNodeMap.Add(serverMode, serverNodeList);
            }
        }

        private void BroadcastKeepAlive()
        {
            var nowTicks = DateTime.Now.Ticks;

            foreach ((var serverMode, List<ServerNode> serverNodes) in m_ServerNodeMap)
            {
                foreach (var serverNode in serverNodes)
                {
                    if (serverNode.IsConnected is false)
                    {
                        continue;
                    }

                    if (serverNode.IsConnecting)
                    {
                        continue;
                    }

                    var keepAlive = new KeepAliveReq
                    {
                        startTimeTick = nowTicks,
                        remoteEndPoint = serverNode.RemoteHost,
                        sourceServerID = ConfigManager.Instance.ServerId,
                    };

                    var result = serverNode.Send(keepAlive);
                    if (result != SendResult.Success)
                    {
                        GameNetworkLog.Critical.LogFatal($"KeepAliveReq failed to send for {serverNode.ServerMode}@{serverNode.RemoteEndPoint}");
                    }
                }
            }
        }

        private void CheckServerNodeClosing()
        {
            var nowTick = DateTime.Now.Ticks;

            foreach (var serverNodeList in m_ServerNodeMap.Values)
            {
                foreach (var serverNode in serverNodeList)
                {
                    var diffTick = nowTick - serverNode.LastKeepAliveTick;
                    var timeSpan = TimeSpan.FromTicks(diffTick);
                    if (timeSpan.TotalMilliseconds > m_KeepAliveCloseLimitMs)
                    {
                        OnServerNodeCloseByKeepAlive(serverNode);
                    }
                }
            }
        }

        private void HandleClientKeepAlive(IContext ctx, KeepAliveRes msg)
        {
            var serverNode = ctx as ServerNode;
            if (serverNode is null)
            {
                throw new Exception($"ctx is not serverNode");
            }

            if (serverNode.ServerID is 0)
            {
                serverNode.SetServerID(msg.serverID);
                CoreLog.Normal.LogInfo($"KeepAliveRes, server id is set for {msg.serverID}");
            }

            serverNode.LastKeepAliveTick = DateTime.Now.Ticks;
            // NetworkLog.Normal.LogInfo($"KeepAlive packet received from {serverNode.ServerMode}@{serverNode.RemoteEndPoint}");
        }

        private void RegisterClientKeepAliveHandler()
        {
            RegisterDispatcher<KeepAliveRes>((ctx, msg) =>
            {
                HandleClientKeepAlive(ctx, msg);
            });
        }

        public void RegisterDispatcher<T>(OnDispatchEventHandler<T> handler)
            where T : IMsg, new()
        {
            m_ClientDispatchManager.RegisterDispatcher<T>((ctx, msg) =>
            {
                var dispatcher = m_HandlerActor.GetDispatcher();
                dispatcher.Post(() =>
                {
                    handler(ctx, msg);
                });
            });
        }

        public void RegisterDispatcherOnce<T>(long targetPacketUID, OnTargetDispatchEventHandler<T> handler)
            where T : ITargetResMsg, new()
        {
            m_ClientDispatchManager.RegisterDispatcherOnce<T>(targetPacketUID, handler);
        }

        public void UnregisterDispatcherOnce<T>(long targetPacketUID)
            where T : ITargetResMsg, new()
        {
            m_ClientDispatchManager.UnregisterDispatcherOnce<T>(targetPacketUID);
        }
    }
}

