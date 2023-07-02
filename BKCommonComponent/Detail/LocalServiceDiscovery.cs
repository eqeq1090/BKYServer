using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BKNetwork.API.LoadBalancer;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKNetwork.Discovery;
using BKNetwork.ConstEnum;

namespace BKCommonComponent.Detail
{
    public sealed class LocalServiceDiscovery : IServiceDiscovery
    {
        private readonly Dictionary<ServiceDiscoveryInfoType, List<HostInfo>> m_HostInfoMap = new Dictionary<ServiceDiscoveryInfoType, List<HostInfo>>();
        public LocalServiceDiscovery()
        {
            m_HostInfoMap.Add(ServiceDiscoveryInfoType.c2g, new()
            {
                new HostInfo()
                {
                    Host = "127.0.0.1",
                    Port = ConfigManager.Instance.GameServerConf!.FromClientPort,
                    HostName = ConfigManager.Instance.CommonConfig.HostName,
                }
            });

            m_HostInfoMap.Add(ServiceDiscoveryInfoType.g2a, new()
            {
                new HostInfo()
                {
                    Host = "127.0.0.1",
                    Port = ConfigManager.Instance.APIServerConf!.Port,
                    HostName = ConfigManager.Instance.CommonConfig.HostName,
                }
            });

            m_HostInfoMap.Add(ServiceDiscoveryInfoType.m2g, new()
            {
                new HostInfo()
                {
                    Host = "127.0.0.1",
                    Port = ConfigManager.Instance.GameServerConf!.FromMatchPort,
                    HostName = ConfigManager.Instance.CommonConfig.HostName,
                }
            });
            m_HostInfoMap.Add(ServiceDiscoveryInfoType.gl2g, new()
            {
                new HostInfo()
                {
                    Host = "127.0.0.1",
                    Port = ConfigManager.Instance.GameServerConf!.FromGlobalPort,
                    HostName = ConfigManager.Instance.CommonConfig.HostName,
                }
            });
        }

        public List<HostInfo> GetTargetServers(ServiceDiscoveryInfoType targetMode)
        {
            if (m_HostInfoMap.ContainsKey(targetMode) is false)
            {
                return new List<HostInfo>(capacity: 0);
            }

            return m_HostInfoMap[targetMode];
        }
    }
}
