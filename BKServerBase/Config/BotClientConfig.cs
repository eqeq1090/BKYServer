using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Config.Server;
using BKServerBase.Config.Storage;
using BKServerBase.ConstEnum;
using BKServerBase.Threading;
using static org.apache.zookeeper.KeeperException;
using Microsoft.Extensions.Configuration;
using BKServerBase.Logger;
using BKServerBase.Util;
using System.Reflection;
using NLog;

namespace BKServerBase.Config
{
    public class BotClientConfigManager : BaseSingleton<BotClientConfigManager>
    {
        private IConfigurationRoot? m_ConfigRoot = null;
        public IConfiguration? Config { get => m_ConfigRoot; }

        public const string BotClientConfigFileName = "local.json";
        private string m_ConfigPath = string.Empty;
        private string m_DefaultPath = Environment.CurrentDirectory;

        public string GameServerHost { get; set; } = string.Empty;
        public int GameServerPort { get; set; }
        public Dictionary<string, int> ScenarioDict = new Dictionary<string, int>();
        public int GroupThreadCount { get; set; } = 1;
        public int TotalUserCount { get; set; } = 1;
        public int AccountIDOffset { get; set; }
        public int ScenarioIterationCount { get; set; } = 1;
        public int FPS { get; set; } = 30;
        public string ConfigPath => m_ConfigPath;

        public bool Initialize()
        {
            if (string.IsNullOrEmpty(ConfigPath))
            {
                m_ConfigPath = FindDefaultConfigPath();
            }

            LoadConfig();

            if (m_ConfigRoot == null)
            {
                throw new Exception("Config Initialize Failed. Terminated");
            }

            LoadCommonConfig();

            return true;
        }
        public string FindDefaultConfigPath()
        {
            return FindDefaultPath("Config");
        }

        private string FindDefaultPath(string folderName)
        {
            var parentDir = new List<string>();
            int depthCount = 3;
            for (int depth = 0; depth < depthCount; ++depth)
            {
                var newDir = Path.Combine(m_DefaultPath, Path.Combine(parentDir.ToArray()), folderName);
                if (Directory.Exists(newDir))
                {
                    return newDir;
                }

                parentDir.Add("..");
            }

            throw new InvalidOperationException($"can not find config path");
        }

        public void LoadCommonConfig()
        {
            GameServerHost = m_ConfigRoot!["GameServerHost"] ?? string.Empty;
            GameServerPort = Convert.ToInt32(m_ConfigRoot["GameServerPort"]);
            foreach (var section in m_ConfigRoot.GetSection("Scenario").GetChildren())
            {
                var name = section["Name"] ?? string.Empty;
                var ratio = Convert.ToInt32(section["Ratio"]);
                if (ScenarioDict.ContainsKey(name) == true)
                {
                    ScenarioDict[name] += ratio;
                }
                else
                {
                    ScenarioDict.Add(name, ratio);
                }
            }
            GroupThreadCount = Convert.ToInt32(m_ConfigRoot["GroupThreadCount"]);
            TotalUserCount = Convert.ToInt32(m_ConfigRoot["TotalUserCount"]);
            AccountIDOffset = Convert.ToInt32(m_ConfigRoot["AccountIDOffset"]);
            ScenarioIterationCount = Convert.ToInt32(m_ConfigRoot["ScenarioIterationCount"]);
        }

        private void LoadConfig()
        {
            //NOTE Log config 설정
            GlobalDiagnosticsContext.Set("service", AppDomain.CurrentDomain.FriendlyName);

            var builder = new ConfigurationBuilder();
            var path = FindDefaultConfigPath();
            builder.AddJsonFile(Path.Combine(path, BotClientConfigFileName));

            m_ConfigRoot = builder.Build();
        }
    }
}
