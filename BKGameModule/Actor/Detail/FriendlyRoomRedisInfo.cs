using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKProtocol;

namespace BKGameServerComponent.Actor.Detail
{
    internal sealed class FriendlyRoomRedisInfo
    {
        public long RoomID { get; set; }
        public int MatchServerID { get; set; }
        public RoomType RoomType { get; set; }
    }
}
