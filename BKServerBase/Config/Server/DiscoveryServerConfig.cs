using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.ConstEnum;
using BKProtocol;

namespace BKServerBase.Config.Server
{
    public sealed class DiscoveryServerConfig
    {
        public readonly int Port;

        public DiscoveryServerConfig(IConfigurationRoot configRoot)
        {
            var section = configRoot.GetSection("DiscoveryServer");
            Port = 9500;//Convert.ToInt32(section["Port"]);
        }   
    }
}
