using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKGameServerComponent.ConstEnum
{
    internal class Consts
    {
        public const int SESSION_MANAGER_RUNNABLE_ID = 10;
        public const int CHANNEL_MANAGER_RUNNABLE_ID = 11;

        public const int CHANNEL_MANAGER_APIQUEUE_SHARD_SIZE = 40;

        public const int CHANNEL_LENGTH = 80;

        public const int COMMON_CHANNEL_SCORE = 500;
        public const int SCORE_PER_PLAYER = 1;

        public const int INVALID_SERVER_ID = -1;

#if DEBUG
        public const int NATIVE_CLIENT_HEART_BEAT_TIMEOUT = 9999999;
#elif RELEASE
        public const int NATIVE_CLIENT_HEART_BEAT_TIMEOUT = 15000;
#endif
    }
}
