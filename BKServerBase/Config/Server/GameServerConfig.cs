using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.ConstEnum;
using BKProtocol;

namespace BKServerBase.Config.Server
{
    public sealed class GameServerConfig
    {
        public readonly int FromClientPort;
        public readonly int FromMatchPort;
        public readonly int FromGlobalPort;
        public readonly int ManagementPort;
        // NOTE(OSCAR) The public ip must be known in order to register the GameServer as a service
        public readonly string PublicIp;
        public readonly int PublicPort;

        public readonly bool UseConfigServer;
        public readonly string ConfigServerHost;
        public readonly string ConfigServerBranch;
        public readonly string ServerType;

        public readonly bool UseApiLB;
        public readonly string ApiLBHost = string.Empty;
        public readonly int ApiLBPort;
        public ImmutableList<string> LBRetryMethods;
        public ImmutableList<HttpStatusCode> LBRetryStatusCodes;
        public readonly int LBRetryMaxCount;
        public readonly int LBRetryFirstBackoffDelay;

        public readonly CompressionType CompressionType;

        public readonly int PrometheusSamplingPeriod;

        public readonly bool Cheat;

        public readonly int APIUserMaxWaitTime;

        public readonly int ApiMaxConnectionPerServer;

        public readonly bool ShowApiBodyLog;

        public readonly bool UseDocker;
        public readonly string DockerIp = string.Empty;
        public readonly int DockerMatchPort;
        public readonly int DockerTeamPort;

        //TODO compression은 추후 추가

        public GameServerConfig(IConfigurationRoot configRoot)
        {
            var section = configRoot.GetSection("GameServer");
            FromClientPort = Convert.ToInt32(section["FromClientPort"]);
            FromMatchPort = Convert.ToInt32(section["FromMatchPort"]);
            FromGlobalPort = Convert.ToInt32(section["FromGlobalPort"]);
            PublicIp = section["PublicIp"] ?? "127.0.0.1";
            PublicPort = Convert.ToInt32(section["PublicPort"]);
            ManagementPort = Convert.ToInt32(section["ManagementPort"]);

            UseConfigServer = Convert.ToBoolean(section["Config:Use"]);
            ConfigServerHost = section["Config:Host"] ?? String.Empty;
            ConfigServerBranch = section["Config:Branch"] ?? String.Empty;
            ServerType = section["Config:ServerType"] ?? String.Empty;
            ShowApiBodyLog = Convert.ToBoolean(section["ShowApiBodyLog"]);
            {
                var builder = ImmutableList.CreateBuilder<string>();
                var convertedArray = section.GetSection("LB:RetryMethod").Get<string[]>();
                if (convertedArray != null)
                {
                    builder.AddRange(convertedArray);
                }
                LBRetryMethods = builder.ToImmutable();
            }
            {
                var builder = ImmutableList.CreateBuilder<HttpStatusCode>();
                var convertedArray = section.GetSection("LB:RetryStatus").Get<HttpStatusCode[]>();
                if (convertedArray != null)
                {
                    builder.AddRange(convertedArray);
                }
                LBRetryStatusCodes = builder.ToImmutable();
            }
            LBRetryMaxCount = Convert.ToInt32(section["LB:RetryCount"]);
            LBRetryFirstBackoffDelay = Convert.ToInt32(section["LB:BackoffDelay"]);

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

            UseDocker = Convert.ToBoolean(section["UseDocker"]);
            if (UseDocker)
            {
                DockerIp = section["DockerIP"] ?? string.Empty;
                DockerMatchPort = Convert.ToInt32(section["DockerMatchPort"]);
                DockerTeamPort = Convert.ToInt32(section["DockerTeamPort"]);
            }
        }

        public bool IsValidPublicIp()
        {
            if (PublicIp.Contains("127.0.0.1") || PublicIp.Contains("localhost"))
            {
                return false;
            }

            return true;
        }
    }
}
