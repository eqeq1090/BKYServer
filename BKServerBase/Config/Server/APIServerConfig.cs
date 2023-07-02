using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;
using System.Data.SqlTypes;
using BKServerBase.Config.Storage;
using BKServerBase.ConstEnum;
using BKProtocol;

namespace BKServerBase.Config.Server
{
    public sealed class MatchmakingConfigName
    {
        private readonly string[] solo;
        private readonly string[] squad3vs3;

        public MatchmakingConfigName(string[] solo, string[] squad3vs3)
        {
            this.solo = solo;
            this.squad3vs3 = squad3vs3;
        }

        public string GetSoloConfigName(int mmrIndex)
        {
            if (mmrIndex < 0)
            {
                throw new Exception($"invalid mmrIndex: {mmrIndex}");
            }

            if (mmrIndex >= solo.Length)
            {
                return solo.Last();
            }

            return solo[mmrIndex];
        }

        public string GetSquad3vs3ConfigName(int mmrIndex)
        {
            if (mmrIndex < 0)
            {
                throw new Exception($"invalid mmrIndex: {mmrIndex}");
            }

            if (mmrIndex >= squad3vs3.Length)
            {
                return squad3vs3.Last();
            }

            return squad3vs3[mmrIndex];
        }
    }

    public sealed class APIServerConfig
    {
        public readonly int Port;
        public readonly int ManagementPort;

        public readonly bool UseConfigServer;
        public readonly string ConfigServerHost;
        public readonly string ConfigServerBranch;
        public readonly string ServerType;

        public readonly bool UseLB;
        public readonly string LBHost;

        public readonly int SystemThreadCountMin;
        public readonly int SystemThreadCountMax;

        public readonly CompressionType CompressionType;

        public readonly int PrometheusSamplingPeriod;

        public readonly bool CheckAuth;

        public readonly Dictionary<RegionCode, MatchmakingConfigName> MatchmakingConfigNameMap = new Dictionary<RegionCode, MatchmakingConfigName>();

        public APIServerConfig(IConfigurationRoot configRoot)
        {
            var section = configRoot.GetSection("APIServer");
            Port = Convert.ToInt32(section["Port"]);
            ManagementPort = Convert.ToInt32(section["ManagementPort"]);

            UseConfigServer = Convert.ToBoolean(section["ConfigServer:Use"]);
            ConfigServerHost = section["ConfigServer:Host"] ?? string.Empty;
            ConfigServerBranch = section["ConfigServer:Branch"] ?? string.Empty;
            ServerType = section["ConfigServer:ServerType"] ?? string.Empty;

            UseLB = Convert.ToBoolean(section["LB:Use"]);
            LBHost = section["LB:Host"] ?? string.Empty;

            SystemThreadCountMin = Convert.ToInt32(section["SystemThreadCount:Min"]);
            SystemThreadCountMax = Convert.ToInt32(section["SystemThreadCount:Max"]);

            PrometheusSamplingPeriod = Convert.ToInt32(section["Prometheus:SamplingPeriod"]);

            section.LoadMatchmakingConfigMap(out MatchmakingConfigNameMap);
            CheckAuth = false;
            //CheckAuth = ConfigManager.Instance.ServerProfile != ServerProfile.Dev &&
            //    ConfigManager.Instance.ServerProfile != ServerProfile.Local &&
            //    ConfigManager.Instance.ServerMode != ServerMode.APIUnitTest;
        }
    }
}
