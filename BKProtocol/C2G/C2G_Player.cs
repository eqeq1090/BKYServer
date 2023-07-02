using MessagePack;
using System.Collections.Generic;

namespace BKProtocol.C2G
{
    [MessagePackObject]
    public class LoginReq : IMsg
    {
        [Key(1)]
        public string accessToken { get; set; } = string.Empty;
        [Key(2)]
        public string DID { get; set; } = string.Empty;
        [Key(3)]
        public long HivePlayerID { get; set; }
        [Key(4)]
        public string email { get; set; } = string.Empty;
        [Key(5)]
        public string quantumCodeVersion { get; set; } = string.Empty;
        [Key(6)]
        public string tableVersion { get; set; } = string.Empty;
        [Key(7)]
        public string protocolVersion { get; set; } = string.Empty;

        public LoginReq() : base(MsgType.CtoG_LoginReq)
        {

        }
    }

    [MessagePackObject]
    public class LoginRes : IResMsg
    {
        [Key(2)]
        public PlayerInfo playerInfo { get; set; } = new PlayerInfo();

        public LoginRes() : base(MsgType.CtoG_LoginRes)
        {

        }
    }

    [MessagePackObject]
    public class LogoutReq : IMsg
    {
        public LogoutReq()
            : base(MsgType.CtoG_LogoutReq)
        {
        }
    }

    [MessagePackObject]
    public class LogoutRes : IResMsg
    {
        public LogoutRes()
            : base(MsgType.CtoG_LogoutRes)
        {
        }
    }

    [MessagePackObject]
    public class ChangeNameReq : IMsg
    {
        [Key(1)]
        public string newName { get; set; } = string.Empty;

        public ChangeNameReq() : base(MsgType.CtoG_ChangeNameReq)
        {

        }
    }

    [MessagePackObject]
    public class ChangeNameRes : IResMsg
    {
        [Key(2)]
        public string changedName { get; set; } = string.Empty;

        public ChangeNameRes() : base(MsgType.CtoG_ChangeNameRes)
        {

        }
    }
    

    [MessagePackObject]
    public class HeartBeatReq : IMsg
    {
        //NOTE 현재는 패킷 body가 없습니다만, 장기적으로는 서버와의 시간 동기화, 정기적으로 받아갈 필요성이 있는 고정형 데이터 등을 여기에 넣을 수 있습니다.

        public HeartBeatReq() : base(MsgType.CtoG_HeartBeatReq)
        {

        }
    }

    [MessagePackObject]
    public class HeartBeatRes : IResMsg
    {
        //NOTE 현재는 패킷 body가 없습니다만, 장기적으로는 서버와의 시간 동기화, 정기적으로 받아갈 필요성이 있는 고정형 데이터 등을 여기에 넣을 수 있습니다.

        public HeartBeatRes() : base(MsgType.CtoG_HeartBeatRes)
        {

        }
    }

    [MessagePackObject]
    public class DisconnectSig : IMsg
    {
        [Key(1)]
        public DisconnectReason reason { get; set; }

        public DisconnectSig() : base(MsgType.CtoG_DisconnectSig)
        {

        }
    }
}
