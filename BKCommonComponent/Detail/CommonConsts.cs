using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKCommonComponent.Detail
{
    public static class CommonConsts
    {

        public const int RedisExpiryUpdateSeconds = 20;
        public const long InGameSessionExpiryMillisec =  60 * 1000;
        public const long RoomSessionExpiryMillisec = 60 * 1000; // 1분
        public const long RoomPlayerSessionExpiryMillisec = 15 * 1000;
        public const int SquadMemberCount = 3;
    }
}
