using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BKProtocol
{
    [MessagePackObject]
    public class InvalidMsg : IMsg
    {
        public InvalidMsg() : base(MsgType.Invalid)
        {
        }
    }


    [MessagePackObject]
    public class PlayerInfo
    {
        [Key(0)]
        public long playerUID { get; set; }
        [Key(1)]
        public bool playerActivated { get; set; }
        [Key(2)]
        public string name { get; set; } = string.Empty;
        [Key(3)]
        public string playerTag { get; set; } = string.Empty;
     
    }

    [MessagePackObject]
    public sealed class PresenceData
    {
        [Key(0)]
        public long PlayerUID { get; set; }

        [Key(1)]
        public LocationType Location { get; set; }

        [Key(3)]
        public DateTime LastDateTime { get; set; }
    }
}
