using MessagePack;
using BKProtocol.Enum;

namespace BKProtocol
{
    public static class ProtocolVersion
    {
        public const string Value = "$ProtocolVersion";
    }

    [MessagePackObject]
    public class IMsg
    {
        [Key(0)]
        public readonly MsgType msgType;

        public IMsg(MsgType msgType)
        {
            this.msgType = msgType;
        }

        [IgnoreMember]
        public ParsingType ParsingType { get; set; }
    }

    [MessagePackObject]
    public class IResMsg : IMsg
    {
        [Key(1)]
        public MsgErrorCode errorCode;

        public IResMsg(MsgType msgType)
            : base(msgType)
        {
        }
    }

    [MessagePackObject]
    public class ITargetMsg : IMsg
    {
        [Key(1)]
        public long targetPacketUID;

        public ITargetMsg(MsgType msgType)
            : base(msgType)
        {
        }
    }

    [MessagePackObject]
    public class ITargetResMsg : IResMsg
    {
        [Key(2)]
        public long targetPacketUID;

        public ITargetResMsg(MsgType msgType)
            : base(msgType)
        {
        }
    }

    [MessagePackObject]
    public class IPubsubMsg
    {
        [Key(0)]
        public PubsubMsgType MsgType;

        public IPubsubMsg(PubsubMsgType msgType)
        {
            MsgType = msgType;
        }
    }

    [MessagePackObject]
    public class IAPIResMsg : IResMsg
    {
        public IAPIResMsg(MsgType msgType)
            : base(msgType)
        {
        }
    }
}
