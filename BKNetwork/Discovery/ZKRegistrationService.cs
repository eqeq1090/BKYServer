using Newtonsoft.Json;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKNetwork.ConstEnum;
using BKProtocol;
using static BKNetwork.API.LoadBalancer;

namespace BKNetwork.Discovery
{
    public class ZKRegistrationService
    {
        private readonly ServiceDiscoveryManager m_ServiceDiscoveryManager;
        private ICommandExecutor m_CommandExecutor;
        private Timer m_ShardCheckTimer;
        private bool m_Initialized = false;

        public ZKRegistrationService(string addr, int port, ServerMode selfMode, Dictionary<ServiceDiscoveryInfoType, bool> resolveServerModes, bool needToRegister)
        {
            m_CommandExecutor = CommandExecutor.CreateCommandExecutor("ZKRegistrationService", 0);
            var list = new List<ServiceDiscoveryManager.ConnectionInfo>();
            if (needToRegister == true)
            {
                list.Add(new ServiceDiscoveryManager.ConnectionInfo()
                {
                    InfoType = selfMode == ServerMode.APIServer ? ServiceDiscoveryInfoType.g2a
                                : ServiceDiscoveryInfoType.Invalid,
                    IpAddress = addr,
                    Port = port,
                });
            }
            m_ServiceDiscoveryManager = new ServiceDiscoveryManager(list, resolveServerModes);
            m_ServiceDiscoveryManager.SetCommandExecutor(m_CommandExecutor);
            m_ShardCheckTimer = new Timer(ExecuteCommandExecutor, null, 0, 1000);
        }

        public void Register()
        {
            m_ServiceDiscoveryManager.Initialize();
            m_Initialized = true;
        }

        private void ExecuteCommandExecutor(object? state)
        {
            if (!m_Initialized)
            {
                return;
            }
            m_CommandExecutor.Execute();
        }

        public List<HostInfo> GetServers(ServiceDiscoveryInfoType mode)
        {
            return m_ServiceDiscoveryManager.ResolveAllHostInfo(mode);
        }
    }
}
