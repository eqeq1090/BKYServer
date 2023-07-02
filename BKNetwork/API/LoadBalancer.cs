using System.Collections.Generic;
using System.Collections.ObjectModel;
using BKServerBase.Config;
using BKServerBase.Logger;
using BKNetwork.Discovery;
using BKNetwork.Dispatch;
using BKProtocol;
using Newtonsoft.Json;

namespace BKNetwork.API
{
    public class LoadBalancer
    {
        public struct HostInfo : IEquatable<HostInfo>
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public string HostName { get; set; }
            public int ManagementPort { get; set; }
            public override bool Equals(object? obj)
            {
                return obj is HostInfo pair && Equals(pair);
            }

            public bool Equals(HostInfo other)
            {
                return Host == other.Host && Port == other.Port;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Host, Port);
            }

            public override string ToString()
            {
                return $"{Host}:{Port}";
            }
        }

        private static ThreadLocal<Random> m_randomGenerator = new ThreadLocal<Random>(() => new Random());
        private HttpClient m_HttpClient;
        private List<HostInfo> m_Servers;
        private Dictionary<HostInfo, int> m_HealthCheckState;
        private List<HostInfo> m_AliveServers;
        private int m_HealthSuccessThreshold = 3;
        private int m_HealthCheckInterval = 2000;
        private bool m_needToHealthCheck;

        public LoadBalancer(bool needToHealthCheck)
        {
            m_HttpClient = new HttpClient(new SocketsHttpHandler());
            m_HttpClient.Timeout = new TimeSpan(0, 0, 0, 0, m_HealthCheckInterval);
            m_Servers = new List<HostInfo>();
            m_HealthCheckState = new Dictionary<HostInfo, int>();
            m_AliveServers = new List<HostInfo>();
            m_needToHealthCheck = needToHealthCheck;
            CheckHealth();

        }
        public void SetServerList(List<HostInfo> serverList)
        {
            Interlocked.Exchange(ref m_Servers, serverList);
        }

        private async Task<bool> CheckHealthSpecificServer(HostInfo hostPortPair)
        {
            var builder = new UriBuilder();
            builder.Path = "/actuator/status";
            builder.Host = hostPortPair.Host;
            builder.Port = hostPortPair.ManagementPort;
            try
            {
                using (var resp = await m_HttpClient.GetAsync(builder.Uri))
                {
                    if (resp.IsSuccessStatusCode == false)
                    {
                        throw new Exception("IsSuccessStatusCode = false");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                CoreLog.Critical.LogWarning($"Failed health check {hostPortPair.Host}: {hostPortPair.Port}: {ex.Message}");
                return false;
            }
        }

        private async Task CheckHealthInternal()
        {
            if (m_needToHealthCheck == false)
            {
                return;
            }
            var servers = m_Servers;
            var toRemove = new List<HostInfo>();
            bool needUpdateAliveServerList = false;
            foreach (var item in m_HealthCheckState.Keys)
            {
                if (servers.Contains(item) == false)
                {
                    toRemove.Add(item);
                    needUpdateAliveServerList = true;
                }
            }

            foreach (var item in toRemove)
            {
                m_HealthCheckState.Remove(item);
            }
            foreach (var item in servers)
            {
                if (item.HostName != ConfigManager.Instance.CommonConfig.HostName)
                {
                    continue;
                }
                int successCount = 0;
                if (m_HealthCheckState.TryGetValue(item, out successCount) == false)
                {
                    successCount = 0;
                }

                var success = await CheckHealthSpecificServer(item);
                if (success == false)
                {
                    if (m_HealthSuccessThreshold == successCount)
                    {
                        CoreLog.Critical.LogWarning($"Removed from health server list (item)");
                        needUpdateAliveServerList = true;
                        m_HealthCheckState[item] = 0;
                    }
                    continue;
                }
                if (m_HealthSuccessThreshold == successCount)
                {
                    continue;
                }
                successCount++;
                m_HealthCheckState[item] = successCount;
                if (m_HealthSuccessThreshold == successCount)
                {
                    CoreLog.Normal.LogDebug($"Success health check fitem)");
                    needUpdateAliveServerList = true;
                }
            }
            if (needUpdateAliveServerList)
            {
                var newList = m_HealthCheckState.Where((x) => x.Value >= m_HealthSuccessThreshold).Select((x) => x.Key).ToList();
                Interlocked.Exchange(ref m_AliveServers, newList);
            }
        }

        private async void CheckHealth()
        {
            while (true)
            {
                try
                {
                    await CheckHealthInternal();
                }
                catch (Exception ex)
                {
                    CoreLog.Critical.LogError(ex);
                }
                await Task.Delay(m_HealthCheckInterval);
            }
        }

        public HostInfo Resolve()
        {
            var aliveServers = m_AliveServers;
            if (aliveServers.Count == 0)
            {
                throw new LoadBalancerException(3, "NoAliveApiNodes");
            }
            var candidateServer = aliveServers.Where(x => x.HostName == ConfigManager.Instance.CommonConfig.HostName);
            if (candidateServer.Count() ==0)
            {
                throw new LoadBalancerException(3, "NoAliveApiNodes");
            }
            var pair = candidateServer.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
            return pair;
        }

        public List<HostInfo> GetAll()
        {
            if (m_needToHealthCheck)
            {
                return m_AliveServers;
            }
            else
            {
                return m_Servers;
            }
        }

        public async Task<HostInfo> ResolveUntil()
        {
            for (int tried = 0; ; tried++)
            {
                try
                {
                    return Resolve();
                }
                catch (Exception e)
                {
                    if (300 <= tried)
                    {
                        CoreLog.Critical.LogError(e);
                    }
                    await Task.Delay(100);
                }
            }
        }
    }
}
