using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;
using BKServerBase.Threading;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Config.Server;
using BKServerBase.Config.Storage;
using CommandLine.Text;
using CommandLine;
using BKProtocol;
using System.Reflection;
using NLog;
using NLog.Conditions;
using NLog.Targets;
using NLog.LayoutRenderers;

namespace BKServerBase.Config
{
    public sealed class ServerOptions
    {
        [Option('n', "id", HelpText = "server id")]
        public int ServerId { get; set; } = 1;

        [Option('m', "mode", HelpText = "setup server mode(required)")]
        public string ServerMode { get; set; } = "AllInOne";

        [Option('p', "profile", HelpText = "setup server profile")]
        public string Profile { get; set; } = "Local";
        [Option('s', "stagetype", HelpText = "setup server stage type")]
        public string StageProfile { get; set; } = "QANormal";
        [Option('l', "location", HelpText = "setup server config location")]
        public string PathName { get; set; } = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)??string.Empty;
    }

    public sealed class ConfigManager : BaseSingleton<ConfigManager>
    {
        
        private IConfigurationRoot m_ConfigRoot = null!;
        private string m_ConfigPath = string.Empty;
        private string m_DefaultPath = string.Empty;
        private ServerProfile m_ServerProfile;
        private ServerMode m_ServerMode;
        private StageServerType m_StageType;
        private int m_ServerId;
        public ImmutableDictionary<string, LoggingEventType> LogLevelOverride = ImmutableDictionary<string, LoggingEventType>.Empty;

        private ConfigManager()
        {
        }

        public string ConfigPath => m_ConfigPath;
        public string DefaultPath => m_DefaultPath;
        public ServerProfile ServerProfile => m_ServerProfile;
        public StageServerType StageServerType => m_StageType;
        public ServerMode ServerMode => m_ServerMode;
        public int ServerId => m_ServerId;
        public CommonConfig CommonConfig { get; private set; } = null!;
        public APIServerConfig? APIServerConf { get; private set; }
        public GameServerConfig? GameServerConf { get; private set; }
        public GlobalServerConfig? GlobalServerConf { get; private set; }
        public DiscoveryServerConfig? DiscoveryServerConf { get; private set; }
        public DBConnectionConfig? DBConnectionConf { get; private set; }
        public RedisConnectionConfig? RedisConnectionConf { get; private set; }
        public MatchServerConfig? MatchServerConfig { get; private set; }

        public string FindDefaultConfigPath()
        {
            return FindDefaultPath("Config");
        }

        public string FindLocalResourcePath()
        {
            var parentDir = new List<string>();
            int depthCount = 6;
            for (int depth = 0; depth < depthCount; ++depth)
            {
                var newDir = Path.Combine(DefaultPath, Path.Combine(parentDir.ToArray()), ConfigManager.Instance.CommonConfig.ResourceLocalPath);
                if (Directory.Exists(newDir))
                {
                    return newDir;
                }
                parentDir.Add("..");
            }
            throw new Exception("Local Resource Path Not Found");
		}

        public string FindDefaultResourceDDLPath()
        {
            var parentDir = new List<string>();
            int depthCount = 6;
            for (int depth = 0; depth < depthCount; ++depth)
            {
                var newDir = Path.Combine(DefaultPath, Path.Combine(parentDir.ToArray()), ConfigManager.Instance.CommonConfig.ResourceLocalPath, "DDL");
                if (Directory.Exists(newDir))
                {
                    return newDir;
                }
                parentDir.Add("..");
            }
            throw new Exception("Local Resource Path Not Found");
        }

        private string FindDefaultPath(string folderName)
        {
            var parentDir = new List<string>();
            int depthCount = 3;
            for (int depth = 0; depth < depthCount; ++depth)
            {
                var newDir = Path.Combine(DefaultPath, Path.Combine(parentDir.ToArray()), folderName);
                if (Directory.Exists(newDir))
                {
                    return newDir;
                }

                parentDir.Add("..");
            }

            throw new InvalidOperationException($"can not find config path");
        }

        private void SetGlobalContextOfLog(ServerProfile serverProflie)
        {
            LayoutRenderer.Register("villservice", (logEvent) => AppDomain.CurrentDomain.FriendlyName);
            LayoutRenderer.Register("villprofile", (logEvent) => m_ServerProfile.ToString());
            LayoutRenderer.Register("villmode", (logEvent) => m_ServerMode.ToString());

            CoreLog.Normal.LogInfo($"Initializing Config. Profile : {m_ServerProfile.ToString()} / m_ServerMode : {m_ServerMode.ToString()}");

            //NOTE Log config 설정
            CoreLog.Normal.LogInfo($"Loading config for profile({serverProflie})..");
        }

        public void SetConfigPath(string path)
        {
            m_ConfigPath = path;

            if (Directory.Exists(path) is false)
            {
                throw new InvalidOperationException($"can not find config path");
            }
        }

        public void Initialize(string[] args)
        {
            var options = Parser.Default.ParseArguments<ServerOptions>(args)
                .WithParsed(LoadActiveProfileConfig);

            if (options.Errors.Count() > 0)
            {
                throw new InvalidOperationException($"CommandLineOption parsing failed");
            }
            if (string.IsNullOrEmpty(ConfigPath))
            {
                m_ConfigPath = FindDefaultConfigPath();
            }
            SetGlobalContextOfLog(ServerProfile);
            

            LoadCommonConfig();
            m_ConfigRoot = LoadServerConfig(ServerMode);
        }

        public bool Shutdown()
        {
            return true;
        }

        public bool OnUpdate(double delta)
        {
            return false;
        }

        private void LoadCommonConfig()
        {
            var builder = new ConfigurationBuilder();

            string configProfile = m_ServerProfile == ServerProfile.QA ? m_StageType.ToString().ToLower() : m_ServerProfile.ToString().ToLower();

            if (m_ServerMode == ServerMode.APIUnitTest)
            {
                builder.AddJsonFile(Path.Combine(m_ConfigPath, BaseConsts.UnitTestPathName, $"{BaseConsts.CommonConfigFileName}_unittest.json"), optional: true);
            }
            else
            {
                builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.CommonConfigFileName}.json"))
                .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.CommonConfigFileName}_local.json"), optional: true)
                .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.CommonConfigFileName}_{configProfile}.json"), optional: true);
            }
            var commonConfigRoot = builder.Build();

            CommonConfig = new CommonConfig(commonConfigRoot, m_ServerProfile);
        }

        private IConfigurationRoot MakeConfigRoot(ServerMode serverMode)
        {
            var builder = new ConfigurationBuilder();

            string configProfile = m_ServerProfile == ServerProfile.QA ? m_StageType.ToString().ToLower() : m_ServerProfile.ToString().ToLower();

            switch (serverMode)
            {
                case ServerMode.AllInOne:
                    {
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.GameServerConfigFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.GameServerConfigFileName}_local.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.APIServerConfigFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.APIServerConfigFileName}_local.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.MatchServerConfigFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.MatchServerConfigFileName}_local.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.DBConnectionConfFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.DBConnectionConfFileName}_local.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.RedisConnectionConfFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.RedisConnectionConfFileName}_local.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.GlobalServerConfFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.GlobalServerConfFileName}_local.json"), optional: true);

                        //환경 변수 기반 로딩
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.GameServerConfigFileName}_{configProfile}.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.APIServerConfigFileName}_{configProfile}.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.MatchServerConfigFileName}_{configProfile}.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.DBConnectionConfFileName}_{configProfile}.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.RedisConnectionConfFileName}_{configProfile}.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.GlobalServerConfFileName}_{configProfile}.json"), optional: true);
                        break;
                    }
                case ServerMode.APIServer:
                    {
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.APIServerConfigFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.APIServerConfigFileName}_local.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.DBConnectionConfFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.DBConnectionConfFileName}_local.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.RedisConnectionConfFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.RedisConnectionConfFileName}_local.json"), optional: true);

                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.APIServerConfigFileName}_{configProfile}.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.DBConnectionConfFileName}_{configProfile}.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.RedisConnectionConfFileName}_{configProfile}.json"), optional: true);
                        break;
                    }
                case ServerMode.GameServer:
                    {
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.GameServerConfigFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.GameServerConfigFileName}_local.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.RedisConnectionConfFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.RedisConnectionConfFileName}_local.json"), optional: true);

                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.GameServerConfigFileName}_{configProfile}.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.RedisConnectionConfFileName}_{configProfile}.json"), optional: true);
                        break;
                    }

                case ServerMode.MatchServer:
                    {
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.RedisConnectionConfFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.RedisConnectionConfFileName}_local.json"), optional: true)
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.RedisConnectionConfFileName}_{configProfile}.json"), optional: true);

                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.MatchServerConfigFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.MatchServerConfigFileName}_local.json"), optional: true)
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.MatchServerConfigFileName}_{configProfile}.json"), optional: true);
                        break;
                    }
                case ServerMode.GlobalServer:
                    {
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, $"{BaseConsts.GlobalServerConfFileName}.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.GlobalServerConfFileName}_local.json"), optional: true)
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", $"{BaseConsts.GlobalServerConfFileName}_{configProfile}.json"), optional: true);
                        break;
                    }
                case ServerMode.APIUnitTest:
                    {
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, BaseConsts.UnitTestPathName, $"{BaseConsts.APIServerConfigFileName}_unittest.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", BaseConsts.UnitTestPathName, $"{BaseConsts.APIServerConfigFileName}_unittest_local.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, BaseConsts.UnitTestPathName, $"{BaseConsts.DBConnectionConfFileName}_unittest.json"))
                           .AddJsonFile(Path.Combine(m_ConfigPath, "custom", BaseConsts.UnitTestPathName, $"{BaseConsts.DBConnectionConfFileName}_unittest_local.json"), optional: true);
                        builder.AddJsonFile(Path.Combine(m_ConfigPath, BaseConsts.UnitTestPathName, $"{BaseConsts.RedisConnectionConfFileName}_unittest.json"))
                            .AddJsonFile(Path.Combine(m_ConfigPath, "custom", BaseConsts.UnitTestPathName, $"{BaseConsts.RedisConnectionConfFileName}_unittest_local.json"), optional: true);
                        break;
                    }
            }

            var configRoot = builder.Build();
            return configRoot;
            // configRoot = MakeRemoteConfig(configRoot);
        }

        private IConfigurationRoot LoadServerConfig(ServerMode serverMode)
        {
            if (serverMode is ServerMode.AllInOne)
            {
                if (m_ServerProfile != ServerProfile.Dev &&
                    m_ServerProfile != ServerProfile.Local)
                {
                    throw new InvalidOperationException($"AllInOne mode is only possible in Dev or Local");
                }
            }

            var configRoot = MakeConfigRoot(serverMode);
            if (configRoot == null)
            {
                throw new InvalidOperationException($"ServerConfigRoot is null");
            }

            switch (ServerMode)
            {
                case ServerMode.APIServer:
                    APIServerConf = new APIServerConfig(configRoot);
                    DBConnectionConf = new DBConnectionConfig(configRoot);
                    RedisConnectionConf = new RedisConnectionConfig(configRoot);
                    break;

                case ServerMode.GameServer:
                    GameServerConf = new GameServerConfig(configRoot);
                    RedisConnectionConf = new RedisConnectionConfig(configRoot);
                    break;

                case ServerMode.AllInOne:
                    MatchServerConfig = new MatchServerConfig(configRoot);
                    GameServerConf = new GameServerConfig(configRoot);
                    APIServerConf = new APIServerConfig(configRoot);
                    DBConnectionConf = new DBConnectionConfig(configRoot);
                    RedisConnectionConf = new RedisConnectionConfig(configRoot);
                    GlobalServerConf = new GlobalServerConfig(configRoot);
                    break;

                case ServerMode.MatchServer:
                    MatchServerConfig = new MatchServerConfig(configRoot);
                    RedisConnectionConf = new RedisConnectionConfig(configRoot);
                    break;

                case ServerMode.GlobalServer:
                    GlobalServerConf = new GlobalServerConfig(configRoot);
                    break;

                case ServerMode.APIUnitTest:
                    APIServerConf = new APIServerConfig(configRoot);
                    DBConnectionConf = new DBConnectionConfig(configRoot);
                    RedisConnectionConf = new RedisConnectionConfig(configRoot);
                    break;

                case ServerMode.DiscoveryServer:
                    DiscoveryServerConf = new DiscoveryServerConfig(configRoot);
                    break;

                case ServerMode.Testor:
                    break;

                default:
                    throw new InvalidOperationException($"ServerMode({serverMode}) is not treated");
            }

            return configRoot;
        }

        public IConfigurationRoot MakeRemoteConfig(IConfigurationRoot initialConfig)
        {
            //컨피그 서버 쓰는지 여부 확인
            //쓴다면 컨피그 서버에 httpclient로 단순히 붙어서 json 하나 받아와서 stream화 하는 함수 구현
            //하단에 AddJsonStream을 jsonfile 뒤에 추가해서 덮어쓰기하여 config를 최종화하여 새로 구움
            //서버 모드에 따라 불러와야 할 config의 수가 달라질 수 있음.
            /*
             * var builder = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(GetConfigDirectory(), "local.json"), optional: true)
                .AddEnvironmentVariables();
             */
            return initialConfig;
        }

        public void Reload()
        {
            if (m_ConfigRoot == null)
            {
                throw new Exception("m_ConfigRoot == null");
            }
            //NOTE ConfigServer로부터 새로 가져오는 것이 필요할 수 있음.
            //구현 후 판단
            m_ConfigRoot.Reload();
            LoadReloadableInfo();
        }

        private void LoadReloadableInfo()
        {
            //NOTE 향후 서버 유형에 따라 맞춰서 리로딩하는 구조 설정 필요
            var logLevelOverrideBuilder = ImmutableDictionary.CreateBuilder<string, LoggingEventType>();
            var logLevelOverrideConfig = m_ConfigRoot!.GetSection("Log:LevelOverride");
            if (logLevelOverrideConfig != null)
            {
                foreach (var level in Enum.GetValues<LoggingEventType>())
                {
                    var values = logLevelOverrideConfig.GetSection(level.ToString())?.Get<string[]>();
                    if (values == null)
                    {
                        continue;
                    }
                    foreach (var methodLine in values)
                    {
                        logLevelOverrideBuilder.TryAdd(methodLine, level);
                    }
                }
            }
            Interlocked.Exchange(ref LogLevelOverride, logLevelOverrideBuilder.ToImmutable());
        }

        public bool UpdateCheatEnabled(bool enabled)
        {
            return true;
        }

        private void LoadActiveProfileConfig(ServerOptions o)
        {
            if (Enum.TryParse<ServerProfile>(o.Profile, true, out m_ServerProfile) is false)
            {
                m_ServerProfile = ServerProfile.Local;
            }

            //stage 타입 읽기
            if(m_ServerProfile == ServerProfile.QA)
            {
                if (Enum.TryParse<StageServerType>(o.StageProfile, true, out m_StageType) is false)
                {
                    m_StageType = StageServerType.QANormal;
                }
            }

            if (Enum.TryParse<ServerMode>(o.ServerMode, true, out m_ServerMode) is false)
            {
                m_ServerMode = ServerMode.AllInOne;
            }

            m_ServerId = o.ServerId;
            m_DefaultPath = o.PathName;
        }

        public int GetManagementPort()
        {
            return ServerMode switch
            {
                ServerMode.MatchServer => ConfigManager.Instance.MatchServerConfig?.ManagementPort ?? ConfigManager.Instance.CommonConfig.ManagementPort,
                ServerMode.APIServer => ConfigManager.Instance.APIServerConf?.ManagementPort ?? ConfigManager.Instance.CommonConfig.ManagementPort,
                ServerMode.GameServer => ConfigManager.Instance.GameServerConf?.ManagementPort ?? ConfigManager.Instance.CommonConfig.ManagementPort,
                ServerMode.GlobalServer => ConfigManager.Instance.GlobalServerConf?.ManagementPort ?? ConfigManager.Instance.CommonConfig.ManagementPort,
                _ => ConfigManager.Instance.CommonConfig.ManagementPort
            };
        }
    }
}
