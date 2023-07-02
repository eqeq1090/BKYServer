using MessagePack;
using BKProtocol.Enum;

namespace BKProtocol.S2S
{
    [MessagePackObject]
    public class KeepAliveReq : IMsg
    {
        [Key(1)]
        public string remoteEndPoint { get; set; } = string.Empty;

        [Key(2)]
        public long startTimeTick { get; set; }

        [Key(3)]
        public int sourceServerID { get; set; }

        public KeepAliveReq()
            : base(MsgType.StoS_KeepAliveReq)
        {
        }
    }

    [MessagePackObject]
    public class KeepAliveRes : IResMsg
    {
        [Key(2)]
        public int serverID { get; set; }

        public KeepAliveRes()
            : base(MsgType.StoS_KeepAliveRes)
        {
        }
    }
}
