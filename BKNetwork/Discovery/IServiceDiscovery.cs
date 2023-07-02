using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BKNetwork.API.LoadBalancer;
using BKServerBase.ConstEnum;
using BKNetwork.ConstEnum;

namespace BKNetwork.Discovery
{
    public interface IServiceDiscovery
    {
        List<HostInfo> GetTargetServers(ServiceDiscoveryInfoType targetMode);
    }
}
