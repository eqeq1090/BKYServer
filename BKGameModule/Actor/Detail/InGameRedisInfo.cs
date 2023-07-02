using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKProtocol;

namespace BKGameServerComponent.Actor.Detail
{
    internal sealed class InGameRedisInfo
    {
        public int MatchServerID { get; set; }
        public long RoomID { get; set; }
        public RoomType RoomType { get; set; }

    }
}
