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
    public sealed class MatchServerConfig
    {
        public readonly int FlexmatchPort;
        public readonly int ManagementPort;

        public readonly bool UseConfigServer;
        public readonly string ConfigServerHost;
        public readonly string ConfigServerBranch;
        public readonly string ServerType;

        public readonly CompressionType CompressionType;

        public readonly int PrometheusSamplingPeriod;

        public readonly bool Cheat;

        public readonly int APIUserMaxWaitTime;

        public readonly int ApiMaxConnectionPerServer;

        public readonly bool ShowApiBodyLog;

        public readonly bool UseApiLB;
        public readonly string ApiLBHost = string.Empty;
        public readonly int ApiLBPort;

        //TODO compression은 추후 추가

        public MatchServerConfig(IConfigurationRoot configRoot)
        {
            var section = configRoot.GetSection("MatchServer");
            if (section == null)
            {
                throw new InvalidOperationException("");
            }
            
            FlexmatchPort = Convert.ToInt32(section["FlexmatchPort"]);
            ManagementPort = Convert.ToInt32(section["ManagementPort"]);

            UseConfigServer = Convert.ToBoolean(section["Config:Use"]);
            ConfigServerHost = section["Config:Host"] ?? String.Empty;
            ConfigServerBranch = section["Config:Branch"] ?? String.Empty;
            ServerType = section["Config:ServerType"] ?? String.Empty;
            ShowApiBodyLog = Convert.ToBoolean(section["ShowApiBodyLog"]);

            PrometheusSamplingPeriod = Convert.ToInt32(section["Prometheus:SamplingPeriod"]);
            Cheat = Convert.ToBoolean(section["Cheat"]);

            APIUserMaxWaitTime = Convert.ToInt32(section["HttpClient:ApiTimeout"]);
            ApiMaxConnectionPerServer = Convert.ToInt32(section["HttpClient:MaxConnection"]);

            UseApiLB = Convert.ToBoolean(section["UseApiLB"]);
            if (UseApiLB)
            {
                var hostPair = section["ApiLBHost"];
                if (hostPair is null)
                {
                    throw new Exception($"ApiLBHost is invalid");
                }

                var splits = hostPair.Split(':');
                if (splits.Length != 2)
                {
                    throw new Exception($"ApiLBHost's length is not 2");
                }

                ApiLBHost = splits[0];
                ApiLBPort = Convert.ToInt32(splits[1]);
            }
        }
    }
}
