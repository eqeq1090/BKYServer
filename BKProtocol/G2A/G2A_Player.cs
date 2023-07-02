using MessagePack;
using System.Collections.Generic;

namespace BKProtocol.G2A
{
    [MessagePackObject]
    public class APILoginReq : IMsg
    {
        [Key(1)]
        public string accessToken { get; set; } = string.Empty;
        [Key(2)]
        public string DID { get; set; } = string.Empty;
        [Key(3)]
        public long HivePlayerID { get; set; }
        [Key(4)]
        public string email { get; set; } = string.Empty;

        public APILoginReq() : base(MsgType.GtoA_LoginReq)
        {

        }
    }

    [MessagePackObject]
    public class APILoginRes : IAPIResMsg
    {

        [Key(2)]
        public string sessionKey { get; set; } = string.Empty;
        [Key(3)]
        public PlayerInfo playerInfo { get; set; } = new PlayerInfo();
        [Key(4)]
        public PresenceData presenceData { get; set; } = new PresenceData();

        public APILoginRes() : base(MsgType.GtoA_LoginRes)
        {

        }
    }

    [MessagePackObject]
    public class APILogoutReq : IMsg
    {
        [Key(1)]
        public long playerUID { get; set; }

        public APILogoutReq()
            : base(MsgType.GtoA_LogoutReq)
        {
        }
    }

    [MessagePackObject]
    public class APILogoutRes : IAPIResMsg
    {
        public APILogoutRes()
            : base(MsgType.GtoA_LogoutRes)
        {
        }
    }

    [MessagePackObject]
    public class APIChangeNameReq : IMsg
    {
        [Key(1)]
        public long playerUID { get; set; }
        [Key(2)]
        public string newName { get; set; } = string.Empty;

        public APIChangeNameReq() : base(MsgType.GtoA_ChangeNameReq)
        {

        }
    }

    [MessagePackObject]
    public class APIChangeNameRes : IAPIResMsg
    {
        [Key(2)]
        public long playerUID { get; set; }
        [Key(3)]
        public string changedName { get; set; } = string.Empty;
        [Key(4)]
        public Dictionary<CurrencyType, int> currencies { get; set; } = new Dictionary<CurrencyType, int>();

        public APIChangeNameRes() : base(MsgType.GtoA_ChangeNameRes)
        {

        }
    }
   
}
