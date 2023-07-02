using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKProtocol.Pubsub
{
    [MessagePackObject]
    public class SyncLoginPlayerMsg : IPubsubMsg
    {
        [Key(1)]
        public int SenderNodeID { get; set; }

        [Key(2)]
        public List<long> PlayerUIDs { get; set; } = new List<long>();

        public SyncLoginPlayerMsg()
            : base(Enum.PubsubMsgType.SyncLoginPlayer)
        {

        }
    }
    [MessagePackObject]
    public class SyncSeasonChangeMsg : IPubsubMsg
    {
        public SyncSeasonChangeMsg()
            : base(Enum.PubsubMsgType.SyncSeasonChange)
        {

        }
    }
    [MessagePackObject]
    public class SyncDailyRefreshMsg : IPubsubMsg
    {
        public SyncDailyRefreshMsg()
            : base(Enum.PubsubMsgType.SyncDailyRefresh)
        {

        }
    }
}
