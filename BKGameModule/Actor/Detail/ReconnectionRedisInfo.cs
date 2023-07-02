using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKProtocol;

namespace BKGameServerComponent.Actor.Detail
{
    internal sealed class ReconnectionRedisInfo
    {
        public LocationType LocationType { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public InGameRedisInfo? InGameInfo { get; set; }
    }
}
