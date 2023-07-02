using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.ConstEnum;

namespace BKServerBase.Config.Server
{
    public sealed class CommonConfig
    {
        // NOTE(OSCAR) This is the zookeeper path where the game servers are registered. ex: "/game-servers"
        public readonly string ZookeeperHost;
        public readonly bool ZookeeperLogging;
        public readonly int ZookeeperUpdatePeriod;

        public readonly int ManagementPort;

        public readonly int FPS;
        public readonly bool NetworkLogFlag;
        public readonly int WorkerThreadCountMin;
        public readonly int IOThreadCountMin;
        public readonly int ServerNodeId;
        public readonly bool ServerKeepAlive;

        public readonly bool UseMinio;
        public readonly string MinioHost;
        public readonly string Bucket;
        public readonly string AccessKey;
        public readonly string SecretKey;
        public readonly string ResourceLocalPath;

        public readonly string HostName;

        public CommonConfig(IConfigurationRoot configRoot, ServerProfile serverProfile)
        {
            ManagementPort = Convert.ToInt32(configRoot["ManagementPort"]);
            if (ManagementPort <= 0)
            {
                ManagementPort = BaseConsts.DEFAULT_MANAGEMENT_PORT;
            }
            ZookeeperUpdatePeriod = 2;
            FPS = Convert.ToInt32(configRoot["FPS"]);
            NetworkLogFlag = Convert.ToBoolean(configRoot["NetworkLogFlag"]);
            ServerNodeId = Convert.ToInt32(configRoot["ServerNodeId"]);

            ZookeeperHost = configRoot["ZK:Host"] ?? string.Empty;
            ZookeeperLogging = Convert.ToBoolean(configRoot["ZK:IsLogging"]);

            WorkerThreadCountMin = Convert.ToInt32(configRoot["WorkerMinThreadCount"]);
            IOThreadCountMin = Convert.ToInt32(configRoot["IOThreadMinCount"]);
            //NOTE API 서버에 직접 붙는 주소정보에 대해서는 위 호스트 정보가 세팅되어 있지 않으면 최소한 LB 정보에는 세팅이 되어있음을 전제로 한다.
            UseMinio = Convert.ToBoolean(configRoot["Minio:Use"] ?? "false");
            MinioHost = configRoot["Minio:Host"] ?? String.Empty;
            Bucket = configRoot["Minio:Bucket"] ?? "latest";
            AccessKey = configRoot["Minio:AccessKey"] ?? String.Empty;
            SecretKey = configRoot["Minio:SecretKey"] ?? String.Empty;
            ResourceLocalPath = configRoot["Minio:LocalPath"] ?? String.Empty;
            ServerKeepAlive = Convert.ToBoolean(configRoot["ServerKeepAlive"]);
            HostName = configRoot["HostName"] ?? String.Empty;

            if (HostName == string.Empty && serverProfile is ServerProfile.Local)
            {
                HostName = Dns.GetHostName();
            } // test 환경에서는 개별 hostName으로 적용해야한다.
        }

        public bool MinioConfigValid()
        {
            return MinioHost != string.Empty &&
                AccessKey != string.Empty &&
                SecretKey != string.Empty;
        }
    }
}
