using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKProtocol.Enum
{
    public enum PubsubMsgType : int
    {
        Invalid = 0,
        ChatFriend,
        ChatTeam,
        FriendRequest,
        FriendAccepted,
        FriendDeleted,
        FriendlyRoomReady,
        FriendlyRoomJoin,
        FriendlyRoomLeave,
        FriendlyRoomMoveSlot,
        FriendlyRoomChangeUnit,
        FriendlyRoomPlayerBanish,
        FriendlyRoomWaitingSlotPlayersBanish,
        FriendlyRoomOfflinePlayer,
        FriendlyRoomSetUpAISetting,
        TeamInvite,
        TeamInviteAccept,
        TeamInviteReject,
        TeamJoinRequest,
        TeamJoinAccept,
        TeamJoinReject,
        TeamLeave,
        TeamKick,
        TeamMemberInfoChange,
        TeamMemberStateChange,
        TeamMemberOnline,
        SyncLoginPlayer,
        SyncSeasonChange,
        SyncDailyRefresh,
    }
}
