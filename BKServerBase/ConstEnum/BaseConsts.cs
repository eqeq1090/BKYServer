using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.ConstEnum
{
    public static class BaseConsts
    {
        public const string CommonConfigFileName = "common_conf";
        public const string GameServerConfigFileName = "game_server_conf";
        public const string APIServerConfigFileName = "api_server_conf";
        public const string MatchServerConfigFileName = "match_server_conf";
        public const string GlobalServerConfFileName = "global_server_conf";

        public const string UnitTestPathName = "unittest";

        public const string DBConnectionConfFileName = "db_connection_conf";
        public const string RedisConnectionConfFileName = "redis_connection_conf";


        public const float MILLISEC_TO_SEC_MAGNIFICANT = 0.001f;
        public const int WORKER_FPS = 30;
        public const int WORKER_FRAME_LENGTH = 1000 / WORKER_FPS;
        public static long COMPONENT_TICK_DURATION_MSEC = WORKER_FRAME_LENGTH / 2;

        public const int SEC_TO_MILLISEC_MAGNIFICANT = 1000;
        public const int MAX_THREADCOORDINATOR_RETRY_COUNT = 10;
        public const int SAMPLING_TICK_COUNT_FOR_SPINWAIT = 2000;

        public const int TIMEOUT_BACKEND_API_SEC = 7;

        public const int DEFAULT_MANAGEMENT_PORT = 22224;

        public const int SessionTimeOutSec = 30;

#if DEBUG
        public const int ClientNodeSendTimeOut = 9999999;
#elif RELEASE
        public const int ClientNodeSendTimeOut = 30000;
#endif
    }
}
