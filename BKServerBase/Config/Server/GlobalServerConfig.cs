using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.ConstEnum;
using BKProtocol;

namespace BKServerBase.Config.Server
{
    public sealed class GlobalServerConfig
    {
        public readonly int ManagementPort;

        public readonly bool UseConfigServer;
        public readonly string ConfigServerHost;
        public readonly string ConfigServerBranch;
        public readonly string ServerType;

        public readonly CompressionType CompressionType;

        public readonly int PrometheusSamplingPeriod;

        public readonly bool Cheat;

        //TODO compression은 추후 추가

        public GlobalServerConfig(IConfigurationRoot configRoot)
        {
            var section = configRoot.GetSection("MatchServer");
            if (section == null)
            {
                throw new InvalidOperationException("");
            }
            
            ManagementPort = Convert.ToInt32(section["ManagementPort"]);

            UseConfigServer = Convert.ToBoolean(section["Config:Use"]);
            ConfigServerHost = section["Config:Host"] ?? String.Empty;
            ConfigServerBranch = section["Config:Branch"] ?? String.Empty;
            ServerType = section["Config:ServerType"] ?? String.Empty;

            PrometheusSamplingPeriod = Convert.ToInt32(section["Prometheus:SamplingPeriod"]);
            Cheat = Convert.ToBoolean(section["Cheat"]);
        }
    }
}
