using System.Data;

namespace BKWebAPIComponent.ConstEnum
{
    internal class Consts
    {
        public const IsolationLevel DEFAULT_DB_ISOLATION_LEVEL = IsolationLevel.RepeatableRead;
        public const string DEFAULT_NAME = "Default_Name";
        public const int COLLECT_REWARD_RECURSIVE_MAX = 3;

        public const string HIVE_DISTRIBUTION_AUTH_URL = "https://auth.globalwithhive.com/game/token/get-token";

        public const string HIVE_AUTH_HEADER = "Authorization";

        public const int SHARD_NUM_EXPIRE_LIMIT = 20000;

        public const int MAX_TEAM_MEMBERS = 3;
        public const int MAX_TEAM_REQUESTS = 10;
        public const int MAX_TEAM_INVITED = 9;

        public const int TEAMID_LENGTH = 8;

        public const int REDIS_TRANSACTION_MAX_RETRY_TIME = 10;

        public const int MISSION_COUNT_PER_DAILY = 4;

        public const int MISSION_CHECK_PAST_DAY_COUNT = 2;

        public const string DEV_IAP_CODE = "DEVIAP";
    }
}
