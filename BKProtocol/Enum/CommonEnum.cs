using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKProtocol
{

    public enum DisconnectReason
    {
        None = 0,
        ByClient = 1,
        SessionError = 2,
        InvalidPacketSize = 3,
        Undefined = 4,
        ByServer = 5,
        DuplicateConnect = 6,
        SessionExpire = 7
    }

    public enum CurrencyType
    {
        Invalid = 0,
        Gold = 1,
        FreeDiamond = 2,
        PaidDiamond = 3,
        HellPoint = 4,
        BattleToken= 5,
        NameChangeTicket = 6,
        PowerShotTicket = 7
    }
    public enum RewardType
    {
        Invalid,
        Currency = 1,
        Pilot = 2,
        PilotExp = 3,
        Frame = 4,
        Emoticon = 5,
        Skin = 6,
        ProfileIcon = 7,
        KillMarker = 8,
        Robot = 9,
        PassPoint = 10,
        Item = 11,
        Trophy = 12,
        RewardGroup = 13,
    }

    public enum RewardOriginateType
    {
        FromOrigin,
        Migrate
    }

    public enum RegionCode
    {
        Local,
        Dev,
        QA,
        PublishQA,
        Asia,
        Europe,
        NA,
        EastAsia,
        KR,
    }

    public enum FlexmatchType : int
    {
        Solo = 1,
        Squad3vs3 = 3,
	}
	
    public enum FriendSearchType
    {
        PlayerName,
        FriendRecommend,
        PlayerTag,
    }
    
	public enum ShopBuyLimitType
    {
        None = 0,
        DailyLimit = 1
    }

    public enum ParsingType
    {
        MsgPack = 1,
        Json,
    }

    public enum RegionType
    {
        Northeast,
    }

    public enum MissionState
    {
        Progress,
        RewardWait,
        Complete,
        Failed,
    }

    public enum ChatType
    {
        Normal = 0,         // 일반
        Quick,              // 퀵
        System,             // 시스템
        Emoticon,           // 이모티콘
        SuggestSwitch,      // 제안 교체
    }

    public enum RoomType
    {
        Friendly,
        FriendlyTest,
        Flexmatch,
    }

    public enum RawEvent : byte
    {
        SendEmoticon,
        SendPing,
        UpdateMapPosition,
        UpdateStartTime,
        GameStart,
        EnableCheat,
        TeamResult,
    }

    public enum LocationType
    {
        Login,
        Lobby,
        InGame,
    }

    public enum RoomSlotType
    {
        Normal,
        Waiting,
    }
}
