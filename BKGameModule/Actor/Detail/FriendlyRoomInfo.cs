using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKProtocol;

namespace BKGameServerComponent.Actor.Detail
{
    internal sealed class FriendlyRoomInfo
    {
        public FriendlyRoomInfo(RoomType roomType, long roomID, string roomCode, int matchServerID)
        {
            RoomType = roomType;
            RoomID = roomID;
            RoomCode = roomCode;
            MatchServerID = matchServerID;
        }

        public string RoomCode { get; }
        public long RoomID { get; }
        public int MatchServerID { get; }
        public RoomType RoomType { get; }
    }
}
