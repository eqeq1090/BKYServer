using System;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using org.apache.zookeeper;
using Microsoft.Extensions.Logging;
using org.apache.utils;
using BKServerBase.Config;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using org.apache.zookeeper.data;
using System.Configuration;
using BKServerBase.Component;
using BKServerBase.Threading;
using BKNetwork.API;
using BKServerBase.Logger;
using BKProtocol;
using static BKNetwork.API.LoadBalancer;
using static org.apache.zookeeper.KeeperException;
using BKServerBase.ConstEnum;
using BKNetwork.ConstEnum;

namespace BKNetwork.Discovery
{
    public class ServiceDiscoveryManager : Watcher, IServiceDiscovery
    {
        private readonly Dictionary<ServiceDiscoveryInfoType, ConnectionInfo> m_ZookeeperBasePathDict = new Dictionary<ServiceDiscoveryInfoType, ConnectionInfo>();
        private ZooKeeper? m_ZooKeeperClient;
        private Dictionary<ServiceDiscoveryInfoType, DiscoveryLoadBalancer> m_TargetHostLoadBalancers = new Dictionary<ServiceDiscoveryInfoType, DiscoveryLoadBalancer>();
        private bool m_Registered;
        private ICommandExecutor? m_CommandExecutor;
        private AtomicFlag m_Running = new AtomicFlag(false);
        public bool Registered => m_Registered;

        public class ConnectionInfo
        {
            public ServiceDiscoveryInfoType InfoType { get; set; }
            public string IpAddress { get; set; } = string.Empty;
            public int Port { get; set; }
            public string BasePathInfo { get; set; } = string.Empty;
        }

        public ServiceDiscoveryManager(List<ConnectionInfo> needToRegisters, Dictionary<ServiceDiscoveryInfoType, bool/*specific targethost*/> serversToCollect)
        {
            foreach (var connectionInfo in needToRegisters) 
            {
                connectionInfo.BasePathInfo = $"/services/{connectionInfo.InfoType}/{ConfigManager.Instance.CommonConfig.HostName}";
                m_ZookeeperBasePathDict.Add(connectionInfo.InfoType, connectionInfo);
            }
            
            foreach (var modekv in serversToCollect)
            {
                m_TargetHostLoadBalancers.Add(modekv.Key, new DiscoveryLoadBalancer(this, $"/services/{modekv.Key}", modekv.Value));
            }
        }

        public void SetCommandExecutor(ICommandExecutor commandExecutor)
        {
            m_CommandExecutor = commandExecutor;
        }

        public void Initialize()
        {
            ConnectToZookeeper();
        }

        public List<HostInfo> GetTargetServers(ServiceDiscoveryInfoType targetMode)
        {
            if (m_TargetHostLoadBalancers.ContainsKey(targetMode) is false)
            {
                return new List<HostInfo>(capacity: 0);
            }

            return m_TargetHostLoadBalancers[targetMode].GetAll();
        }

        private void ConnectToZookeeper()
        {
            m_Registered = false;
            var connString = ConfigManager.Instance.CommonConfig.ZookeeperHost;
            CoreLog.Normal.LogDebug($"Using zookeeper connection string : {connString}");
            m_ZooKeeperClient = new ZooKeeper(connString, 5000, this);
        }

        public HostInfo ResolveAPIS()
        {
            switch (ConfigManager.Instance.ServerMode)
            {
                case ServerMode.GameServer:
                    if (ConfigManager.Instance.GameServerConf!.UseApiLB)
                    {
                        return new HostInfo()
                        {
                            Host = ConfigManager.Instance.GameServerConf!.ApiLBHost,
                            Port = ConfigManager.Instance.GameServerConf!.ApiLBPort,
                        };
                    }
                    break;

                case ServerMode.MatchServer:
                    if (ConfigManager.Instance.MatchServerConfig!.UseApiLB)
                    {
                        return new HostInfo()
                        {
                            Host = ConfigManager.Instance.MatchServerConfig!.ApiLBHost,
                            Port = ConfigManager.Instance.MatchServerConfig!.ApiLBPort,
                        };
                    }
                    break;

                case ServerMode.AllInOne:
                    {
                        return new HostInfo()
                        {
                            Host = "127.0.0.1",
                            Port = ConfigManager.Instance.APIServerConf!.Port,
                        };
                    }
            }
            
            if (m_TargetHostLoadBalancers.Count == 0)
            {
                throw new InvalidOperationException("Resolve API not permitted on m_TargetHostLoadBalancer not initialized");
            }

            return m_TargetHostLoadBalancers[ServiceDiscoveryInfoType.g2a].Resolve();
        }

        public List<HostInfo> ResolveAllHostInfo(ServiceDiscoveryInfoType mode)
        {

            if (m_TargetHostLoadBalancers.Count == 0)
            {
                CoreLog.Critical.LogWarning("Resolve API not permitted on m_TargetHostLoadBalancer not initialized");
                return new List<HostInfo>();
            }
            return m_TargetHostLoadBalancers[mode].GetAll();
            
        }

        public override async Task process(WatchedEvent @event)
        {
            CoreLog.Normal.LogDebug(@event.ToString());
            if (@event.get_Type() != Event.EventType.None)
            {
                return;
            }
            switch (@event.getState())
            {
                case Event.KeeperState.SyncConnected:
                    m_CommandExecutor?.Invoke(() =>
                    {
                        if (m_ZooKeeperClient == null)
                        {
                            return;
                        }
                        if (m_TargetHostLoadBalancers.Count > 0)
                        {
                            foreach (var item in m_TargetHostLoadBalancers)
                            {
                                item.Value.Initialize();
                            }
                        }

                        if (m_Registered == false && m_ZookeeperBasePathDict.Count > 0)
                        {
                            UpdateServer();
                        }
                    });
                    break;
                case Event.KeeperState.Disconnected:
                    m_CommandExecutor?.Invoke(async () =>
                    {
                        try
                        {
                            m_Registered = false;
                            await m_ZooKeeperClient!.closeAsync();
                        }
                        catch (Exception e)
                        {
                            CoreLog.Critical.LogFatal(e);
                        }
                        ConnectToZookeeper();
                    });
                    break;
                default:
                    break;
            }
            await Task.CompletedTask;
        }

        private async Task CreateBasePathAsync(string basePath, List<ACL> acl, CreateMode createMode)
        {
            try
            {
                if (await m_ZooKeeperClient!.existsAsync(basePath) != null)
                {
                    return;
                }
                var paths = basePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var currentPath = "/";
                foreach (var path in paths)
                {
                    currentPath += path;
                    if (await m_ZooKeeperClient!.existsAsync(currentPath) == null)
                    {
                        await m_ZooKeeperClient!.createAsync(currentPath, null, acl, createMode);
                    }
                    currentPath += "/";
                }
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogFatal(e);
            }
        }

        private string CreateGameServerNodeData(ServiceDiscoveryInfoType infoType)
        {
            if (m_ZookeeperBasePathDict.TryGetValue(infoType, out var connectionInfo) == false)
            {
                return string.Empty;
            }
            return JsonConvert.SerializeObject(new
            {
                address = connectionInfo.IpAddress,
                port = connectionInfo.Port,
                hostName = ConfigManager.Instance.CommonConfig.HostName,
                managementPort = ConfigManager.Instance.GetManagementPort(),
                //ip = address?.ip,
                //serverID = ConfigurationManager.Instance.ServerNodeID,
                //commithash = Configuration.BuildVersion.IsEnabled ? Configuration.BuildVersion.GitCommitHash : "",
                //buildTime = Configuration.BuildVersion.IsEnabled ? Configuration.BuildVersion.BuildTime : "",
                //branchName = Configuration.BuildVersion.IsEnabled ? Configuration.BuildVersion.GitBranchName : "",
                //buildNumber = Configuration.BuildVersion.IsEnabled ? Configuration.BuildVersion.BuildNumber : "",
                //protocolVersion = "",//version
                //name = Configuration.ConfigurationManager.Instance!.MachineName,
                //minioBucket = ConfigurationManager.Instance!.MinioBucket,
                //tag = ConfigurationManager.Instance!.Tag,
                //serverType = ConfigurationManager.Instance!.ServerType,
                //maxUserCount = ConfigurationManager.Instance!.MaxUserCount,
                //currentUserCount = 0, //실제로는 게임서버 노드에서 집계된걸 써야할지도
                //updateseq = 0,
            });
        }

        private async Task RegisterServer()
        {
            try
            {
                foreach (var item in m_ZookeeperBasePathDict.Values)
                {
                    await CreateBasePathAsync(item.BasePathInfo, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);

                    var nodeData = CreateGameServerNodeData(item.InfoType);

                    var fullPath = $"{item.BasePathInfo}/{ConfigManager.Instance.ServerId}";

                    if (await m_ZooKeeperClient!.existsAsync(fullPath) == null)
                    {
                        await m_ZooKeeperClient!.createAsync(fullPath, Encoding.UTF8.GetBytes(nodeData), ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL);
                    }
                    m_Registered = true;
                }
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogFatal(e);
            }
        }

        private async void UpdateServer()
        {
            if (m_Running.On() == false)
            {
                return;
            }
            while (true)
            {
                await Task.Delay(ConfigManager.Instance.CommonConfig.ZookeeperUpdatePeriod * 1000);
                if (m_ZooKeeperClient!.getState() != ZooKeeper.States.CONNECTED)
                {
                    continue;
                }
                foreach (var item in m_ZookeeperBasePathDict.Values)
                {
                    var fullPath = $"{item.BasePathInfo}/{ConfigManager.Instance.CommonConfig.HostName}";
                    var nodeData = CreateGameServerNodeData(item.InfoType);
                    try
                    {
                        if (await m_ZooKeeperClient!.existsAsync(fullPath) == null)
                        {
                            await m_ZooKeeperClient!.createAsync(fullPath, Encoding.UTF8.GetBytes(nodeData), ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL);
                        }
                        else
                        {
                            await m_ZooKeeperClient!.setDataAsync(fullPath, Encoding.UTF8.GetBytes(nodeData));
                        }
                    }
                    catch (org.apache.zookeeper.KeeperException.NoNodeException)
                    {
                        await RegisterServer();
                    }
                    catch (Exception ex)
                    {
                        CoreLog.Normal.LogWarning(ex);
                    }
                }
            }
        }

        public class ChildWatcher : Watcher
        {
            private ServiceDiscoveryManager m_manager;
            private LoadBalancer m_LoadBalancer;
            private string m_Path;

            public ChildWatcher(ServiceDiscoveryManager manager, LoadBalancer lb, string path)
            {
                m_manager = manager;
                m_LoadBalancer = lb;
                m_Path = path;
            }
            public async void Initialize()
            {
                try
                {
                    await Update();
                }
                catch (Exception ex)
                {
                    CoreLog.Critical.LogFatal(ex);
                }
            }

            private async Task Update()
            {
                var client = m_manager.m_ZooKeeperClient;
                var stat = await client!.existsAsync(m_Path, this);
                if (stat == null)
                {
                    return;
                }

                var children = await client.getChildrenAsync(m_Path, this);
                var newServerList = new List<LoadBalancer.HostInfo>();
                foreach (var child in children.Children)
                {
                    var subChildren = await client.getChildrenAsync($"{m_Path}/{child}", this);
                    foreach (var subChild in subChildren.Children)
                    {
                        try
                        {
                            var data = await client.getDataAsync($"{m_Path}/{child}/{subChild}");
                            var jsonObject = JObject.Parse(Encoding.UTF8.GetString(data.Data));
                            var address = jsonObject["address"];
                            var hostName = jsonObject["hostName"]!.ToString();
                            newServerList.Add(new LoadBalancer.HostInfo
                            {
                                Host = jsonObject["address"]!.ToString(),
                                Port = jsonObject["port"]!.ToObject<int>(),
                                HostName = hostName,
                                ManagementPort = jsonObject["managementPort"]!.ToObject<int>(),
                            });
                        }
                        catch (Exception ex)
                        {
                            CoreLog.Critical.LogFatal(ex);
                        }
                    }
                }
                CoreLog.Normal.LogDebug($"Found (newServerList.Count) servers from zookeeper (mPath)");
                m_LoadBalancer.SetServerList(newServerList);
            }

            public override async Task process(WatchedEvent Gevent)
            {
                CoreLog.Normal.LogDebug(Gevent.ToString());
                switch (Gevent.get_Type())
                {
                    case Event.EventType.NodeCreated:
                    case Event.EventType.NodeChildrenChanged:
                        await Update();
                        break;
                }
            }
        }

        public class DiscoveryLoadBalancer : LoadBalancer
        {
            private ChildWatcher watcher;
            public DiscoveryLoadBalancer(ServiceDiscoveryManager manager, string path, bool needToHealthCheck)
                : base(needToHealthCheck)
            {
                watcher = new ChildWatcher(manager, this, path);
            }

            public void Initialize()
            {
                watcher.Initialize();
            }
        }
    }
}
