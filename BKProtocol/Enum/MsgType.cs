namespace BKProtocol
{
    public enum MsgType : int
    {
        Invalid = 0,

        ////////////////////////////////////////////////////
        /// Client<->Game
        ////////////////////////////////////////////////////
        ///
        //Player
        CtoG_LoginReq = 1000001,
        CtoG_LoginRes = 1000002,
        CtoG_GetPlayerInfoReq = 1000003,
        CtoG_GetPlayerInfoRes = 1000004,
        CtoG_PilotLevelUpReq = 1000007,
        CtoG_PilotLevelUpRes = 1000008,
        CtoG_SelectPilotReq = 1000009,
        CtoG_SelectPilotRes = 1000010,
        CtoG_SelectRobotReq = 1000011,
        CtoG_SelectRobotRes = 1000012,
        CtoG_SelectProfileIconReq = 1000013,
        CtoG_SelectProfileIconRes = 1000014,
        CtoG_SelectKillMarkerReq = 1000015,
        CtoG_SelectKillMarkerRes = 1000016,
        CtoG_ChangeEmoticonSlotReq = 1000017,
        CtoG_ChangeEmoticonSlotRes = 1000018,
        CtoG_SelectFrameReq = 1000019,
        CtoG_SelectFrameRes = 1000020,
        CtoG_ChangeNameReq = 1000021,
        CtoG_ChangeNameRes = 1000022,
        CtoG_ChangePilotInfoSig = 1000025,
        CtoG_ChangeCurrencySig = 1000026,
        CtoG_ChangeSeasonInfoSig = 1000027,
        CtoG_ChangePlayerStatusSig = 1000028,
        CtoG_QueryBattleHistoryReq = 1000029,
        CtoG_QueryBattleHistoryRes = 1000030,
        CtoG_ChangeSkinSig = 1000031,
        CtoG_ChangeFrameSig = 1000032,
        CtoG_ChangeKillMarkerSig = 1000033,
        CtoG_ChangeEmoticonSig = 1000034,
        CtoG_ChangeProfileIconSig = 1000035,
        CtoG_ChangeRobotSig = 1000036,
        CtoG_HeartBeatReq = 1000037,
        CtoG_HeartBeatRes = 1000038,
        CtoG_CompositeSelectReq = 1000039,
        CtoG_CompositeSelectRes = 1000040,
        CtoG_DisconnectSig = 1000041,
        CtoG_LogoutReq = 1000042,
        CtoG_LogoutRes = 1000043,

        //Season,Reward
        CtoG_TrophyRewardPlayerReq = 1000101,
        CtoG_TrophyRewardPlayerRes = 1000102,
        CtoG_TrophyRewardPilotReq = 1000103,
        CtoG_TrophyRewardPilotRes = 1000104,
        CtoG_SeasonPassRewardReq = 1000105,
        CtoG_SeasonPassRewardRes = 1000106,
        CtoG_StartSeasonPassReq = 1000107,
        CtoG_StartSeasonPassRes = 1000108,
        CtoG_BuyPassPointReq = 1000109,
        CtoG_BuyPassPointRes = 1000110,
        CtoG_GetSeasonInfoReq = 1000111,
        CtoG_GetSeasonInfoRes = 1000112,

        //Shop
        CtoG_PurchaseShopItemReq = 1000201,
        CtoG_PurchaseShopItemRes = 1000202,
        CtoG_IAPReq = 1000203,
        CtoG_IAPRes = 1000204,
        CtoG_PurchaseDailyShopItemReq = 1000205,
        CtoG_PurchaseDailyShopItemRes = 1000206,

        //Skin
        CtoG_EquipRobotSkinReq = 1000301,
        CtoG_EquipRobotSkinRes = 1000302,
        CtoG_EquipPilotSkinReq = 1000303,
        CtoG_EquipPilotSkinRes = 1000304,

        //Match
        CtoG_StartMatchmakingReq = 1000401,
        CtoG_StartMatchmakingRes = 1000402,
        CtoG_StopMatchmakingReq = 1000403,
        CtoG_StopMatchmakingRes = 1000404,
        CtoG_CreateTeamCodeReq = 1000405,
        CtoG_CreateTeamCodeRes = 1000406,
        CtoG_CompleteMatchmakingSig = 1000407,

        //Friend
        CtoG_RequestFriendReq = 1000501,
        CtoG_RequestFriendRes = 1000502,
        CtoG_AcceptFriendReq = 1000503,
        CtoG_AcceptFriendRes = 1000504,
        CtoG_RejectFriendReq = 1000505,
        CtoG_RejectFriendRes = 1000506,
        CtoG_DeleteFriendReq = 1000507,
        CtoG_DeleteFriendRes = 1000508,
        CtoG_GetFriendInfoListReq = 1000509,
        CtoG_GetFriendInfoListRes = 1000510,
        CtoG_GetFriendInfoReq = 1000511,
        CtoG_GetFriendInfoRes = 1000512,
        CtoG_GetFriendRequestListReq = 1000513,
        CtoG_GetFriendRequestListRes = 1000514,
        CtoG_GetFriendRecommendListReq = 1000515,
        CtoG_GetFriendRecommendListRes = 1000516,
        CtoG_AddFriendRecommendReq = 1000517,
        CtoG_AddFriendRecommendRes = 1000518,
        CtoG_SearchFriendReq = 1000519,
        CtoG_SearchFriendRes = 1000520,
        CtoG_GetFriendInfoDetailReq = 1000521,
        CtoG_GetFriendInfoDetailRes = 1000522,

        //Game
        CtoG_EndGamePlayerSig = 1000701,
        CtoG_MoveSceneLocationReq = 1000702,
        CtoG_MoveSceneLocationRes = 1000703,

        //Team
        CtoG_CreateTeamReq = 1000800,
        CtoG_CreateTeamRes = 1000801,
        CtoG_JoinTeamReq = 1000802,
        CtoG_JoinTeamRes = 1000803,
        CtoG_LeaveTeamReq = 1000804,
        CtoG_LeaveTeamRes = 1000805,
        CtoG_InviteTeamReq = 1000806,
        CtoG_InviteTeamRes = 1000807,
        CtoG_InviteTeamAcceptReq = 1000808,
        CtoG_InviteTeamAcceptRes = 1000809,
        CtoG_InviteTeamRejectReq = 1000810,
        CtoG_InviteTeamRejectRes = 1000811,
        CtoG_JoinTeamRequestReq = 1000812,
        CtoG_JoinTeamRequestRes = 1000813,
        CtoG_AcceptTeamJoinReq = 1000814,
        CtoG_AcceptTeamJoinRes = 1000815,
        CtoG_RejectTeamJoinReq = 1000816,
        CtoG_RejectTeamJoinRes = 1000817,
        CtoG_GetPlayerTeamInfoReq = 1000818,
        CtoG_GetPlayerTeamInfoRes = 1000819,
        CtoG_KickTeamReq = 1000820,
        CtoG_KickTeamRes = 1000821,

        //Team Sig
        CtoG_TeamInviteSig = 1000851,
        CtoG_TeamInviteAcceptSig = 1000852,
        CtoG_TeamInviteAcceptToTeammateSig = 1000853,
        CtoG_TeamInviteRejectSig = 1000854,
        CtoG_TeamInviteRejectToTeammateSig = 1000855,
        CtoG_TeamJoinRequestSig = 1000856,
        CtoG_TeamJoinRequestAcceptSig = 1000857,
        CtoG_TeamJoinRequestAcceptToMembersSig = 1000858,
        CtoG_TeamJoinRequestRejectSig = 1000859,
        CtoG_TeamLeaveSig = 1000861,
        CtoG_TeamKickSig = 1000862,
        CtoG_TeamKickToTeammateSig = 1000863,
        CtoG_TeamMemberChangeInfoSig = 1000864,
        CtoG_TeamStateChangeSig = 1000865,
        CtoG_TeamOfflineSig = 1000866,

        //Chat
        CtoG_ChatFriendReq = 1000901,
        CtoG_ChatFriendRes = 1000902,
        CtoG_ChatFriendSig = 1000903,
        CtoG_ChatTeamReq = 1000904,
        CtoG_ChatTeamRes = 1000905,
        CtoG_ChatTeamSig = 1000906,

        //Mission
        CtoG_CompleteMissionReq = 1001001,
        CtoG_CompleteMissionRes = 1001002,
        CtoG_ChangeMissionSig = 1001003,
        CtoG_ChangeMissionNoticeStateReq = 1001004,
        CtoG_ChangeMissionNoticeStateRes = 1001005,
        CtoG_RefreshMissionReq = 1001006,
        CtoG_RefreshMissionRes = 1001007,

        //Cheat
        CtoG_AdminCommandReq = 1010001,
        CtoG_AdminCommandRes = 1010002,

        //Room
        CtoG_CreateRoomReq = 1020001,
        CtoG_CreateRoomRes = 1020002,
        CtoG_JoinRoomReq = 1020003,
        CtoG_JoinRoomRes = 1020004,
        CtoG_LeaveRoomReq = 1020005,
        CtoG_LeaveRoomRes = 1020006,
        CtoG_ReadyRoomReq = 1020007,
        CtoG_ReadyRoomRes = 1020008,
        CtoG_StartRoomReq = 1020009,
        CtoG_StartRoomRes = 1020010,
        CtoG_MoveRoomSlotReq = 1020011,
        CtoG_MoveRoomSlotRes = 1020012,
        CtoG_BanishRoomPlayerReq = 1020013,
        CtoG_BanishRoomPlayerRes = 1020014,
        CtoG_SetUpRoomAISettingReq = 1020015,
        CtoG_SetUpRoomAISettingRes = 1020016,

        ////////////////////////////////////////////////////
        /// Game -> Client
        ////////////////////////////////////////////////////
        GtoC_AlertFriendRequestSig = 1030000,
        GtoC_AlertFriendAcceptSig = 1030001,
        GtoC_AlertFriendDeleteSig = 1030002,
        GtoC_ReadyRoomSig = 1030003,
        GtoC_JoinRoomSig = 1030004,
        GtoC_EndGameRoomSig = 1030005,
        GtoC_LeaveRoomSig = 1030006,
        GtoC_MoveRoomSlotSig = 1030007,
        GtoC_ChangeRoomUnitSig = 1030008,
        GtoC_BanishRoomPlayerSig = 1030009,
        GtoC_BanishWaitingSlotPlayersSig = 1030010,
        GtoC_FailStartingGameSig = 1030011,
        GtoC_OfflineRoomPlayerSig = 1030012,
        GtoC_SetUpRoomAISettingSig = 1030013,
        GtoC_RefreshChangeSig = 1030014,
        GtoC_RefreshDailyShopSig = 1030015,
        GtoC_RefreshMissionSig = 1030016,

        //시스템
        GtoC_AlertSeasonChangeSig = 1040001,

        //TODO 미구현 프로토콜들
        //BattleHistory
        //LeaderBoard

        ////////////////////////////////////////////////////
        /// Game <->API
        ////////////////////////////////////////////////////

        //Player
        GtoA_LoginReq = 2000001,
        GtoA_LoginRes = 2000002,
        GtoA_GetPlayerInfoReq = 2000003,
        GtoA_GetPlayerInfoRes = 2000004,
        GtoA_PilotLevelUpReq = 2000007,
        GtoA_PilotLevelUpRes = 2000008,
        GtoA_SelectPilotReq = 2000009,
        GtoA_SelectPilotRes = 2000010,
        GtoA_SelectRobotReq = 2000011,
        GtoA_SelectRobotRes = 2000012,
        GtoA_SelectProfileIconReq = 2000013,
        GtoA_SelectProfileIconRes = 2000014,
        GtoA_SelectKillMarkerReq = 2000015,
        GtoA_SelectKillMarkerRes = 2000016,
        GtoA_ChangeEmoticonSlotReq = 2000017,
        GtoA_ChangeEmoticonSlotRes = 2000018,
        GtoA_SelectFrameReq = 2000019,
        GtoA_SelectFrameRes = 2000020,
        GtoA_ChangeNameReq = 2000021,
        GtoA_ChangeNameRes = 2000022,
        GtoA_CompositeSelectReq = 2000023,
        GtoA_CompositeSelectRes = 2000024,
        GtoA_LogoutReq = 2000025,
        GtoA_LogoutRes = 2000026,

        //Season,Reward
        GtoA_TrophyRewardPlayerReq = 2000101,
        GtoA_TrophyRewardPlayerRes = 2000102,
        GtoA_TrophyRewardPilotReq = 2000103,
        GtoA_TrophyRewardPilotRes = 2000104,
        GtoA_SeasonPassRewardReq = 2000105,
        GtoA_SeasonPassRewardRes = 2000106,
        GtoA_StartSeasonPassReq = 2000107,
        GtoA_StartSeasonPassRes = 2000108,
        GtoA_BuyPassPointReq = 2000109,
        GtoA_BuyPassPointRes = 2000110,
        GtoA_GetSeasonInfoReq = 2000111,
        GtoA_GetSeasonInfoRes = 2000112,

        //Shop
        GtoA_PurchaseShopItemReq = 2000201,
        GtoA_PurchaseShopItemRes = 2000202,
        GtoA_IAPReq = 2000203,
        GtoA_IAPRes = 2000204,
        GtoA_PurchaseDailyShopItemReq = 2000205,
        GtoA_PurchaseDailyShopItemRes = 2000206,

        //Skin
        GtoA_EquipRobotSkinReq = 2000301,
        GtoA_EquipRobotSkinRes = 2000302,
        GtoA_EquipPilotSkinReq = 2000303,
        GtoA_EquipPilotSkinRes = 2000304,

        //Match
        GtoA_StartMatchmakingReq = 2000401,
        GtoA_StartMatchmakingRes = 2000402,
        GtoA_StopMatchmakingReq = 2000403,
        GtoA_StopMatchmakingRes = 2000404,

        //Friend
        GtoA_RequestFriendReq = 2000501,
        GtoA_RequestFriendRes = 2000502,
        GtoA_AcceptFriendReq = 2000503,
        GtoA_AcceptFriendRes = 2000504,
        GtoA_RejectFriendReq = 2000505,
        GtoA_RejectFriendRes = 2000506,
        GtoA_DeleteFriendReq = 2000507,
        GtoA_DeleteFriendRes = 2000508,
        GtoA_GetFriendInfoListReq = 2000509,
        GtoA_GetFriendInfoListRes = 2000510,
        GtoA_GetFriendInfoReq = 2000511,
        GtoA_GetFriendInfoRes = 2000512,
        GtoA_GetFriendRequestListReq = 2000513,
        GtoA_GetFriendRequestListRes = 2000514,
        GtoA_GetFriendRecommendListReq = 2000515,
        GtoA_GetFriendRecommendListRes = 2000516,
        GtoA_AddFriendRecommendReq = 2000517,
        GtoA_AddFriendRecommendRes = 2000518,
        GtoA_SearchFriendReq = 2000519,
        GtoA_SearchFriendRes = 2000520,
        GtoA_GetFriendInfoDetailReq = 2000521,
        GtoA_GetFriendInfoDetailRes = 2000522,

        //Game
        GtoA_APIEndGamePlayerReq = 2000601,
        GtoA_APIEndGamePlayerRes = 2000602,
        GtoA_BattleHistoryReq = 2000603,
        GtoA_BattleHistoryRes = 2000604,

        //Team
        GtoGL_CreateTeamReq = 2000701,
        GtoGL_CreateTeamRes = 2000702,
        GtoGL_JoinTeamReq = 2000705,
        GtoGL_JoinTeamRes = 2000706,
        GtoGL_LeaveTeamReq = 2000707,
        GtoGL_LeaveTeamRes = 2000708,
        GtoGL_InviteTeamReq = 2000709,
        GtoGL_InviteTeamRes = 2000710,
        GtoGL_InviteTeamAcceptReq = 2000711,
        GtoGL_InviteTeamAcceptRes = 2000712,
        GtoGL_InviteTeamRejectReq = 2000713,
        GtoGL_InviteTeamRejectRes = 2000714,
        GtoGL_JoinTeamRequestReq = 2000715,
        GtoGL_JoinTeamRequestRes = 2000716,
        GtoGL_AcceptTeamJoinReq = 2000717,
        GtoGL_AcceptTeamJoinRes = 2000718,
        GtoGL_RejectTeamJoinReq = 2000719,
        GtoGL_RejectTeamJoinRes = 2000720,
        GtoGL_GetTeamMemberCandidatesReq = 2000721,
        GtoGL_GetTeamMemberCandidatesRes = 2000722,
        GtoGL_GetTeamCandidatesReq = 2000723,
        GtoGL_GetTeamCandidatesRes = 2000724,
        GtoGL_GetPlayerTeamInfoReq = 2000725,
        GtoGL_GetPlayerTeamInfoRes = 2000726,
        GtoGL_KickTeamReq = 2000727,
        GtoGL_KickTeamRes = 2000728,
        GtoGL_TSTeamLeaveSig = 2000731,
        GtoGL_TSTeamKickSig = 2000732,
        GtoGL_TSTeamInviteSig = 2000733,
        GtoGL_TSTeamInviteAcceptSig = 2000734,
        GtoGL_TSTeamInviteRejectSig = 2000735,
        GtoGL_TSTeamJoinRequestSig = 2000736,
        GtoGL_TSTeamJoinRequestAcceptSig = 2000737,
        GtoGL_TSTeamJoinRequestRejectSig = 2000738,
        GtoGL_ExireSessionSig = 2000739,
        GtoGL_TSTeamMemberInfoReq = 2000740,
        GtoGL_TSTeamMemberInfoRes = 2000741,
        GtoGL_CheckSeasonSig = 2000742,
        GtoGL_DailyRefreshSig = 2000743,
        GtoGL_TSTeamMemberCandidateInfoReq = 2000744,
        GtoGL_TSTeamMemberCandidateInfoRes = 2000745,
        GtoGL_TSTeamJoinRequestAcceptToMembersSig = 2000746,

        //Mission
        GtoA_APIChangeMissionReq = 2000801,
        GtoA_APIChangeMissionRes = 2000802,
        GtoA_CompleteMissionReq = 2000803,
        GtoA_CompleteMissionRes = 2000804,
        GtoA_ChangeMissionNoticeStateReq = 2000805,
        GtoA_ChangeMissionNoticeStateRes = 2000806,
        GtoA_RefreshMissionReq = 2000807,
        GtoA_RefreshMissionRes = 2000808,

        //Admin
        GtoA_AdminAddPilotTrophyReq = 2000901,
        GtoA_AdminAddPilotTrophyRes = 2000902,
        GtoA_AdminAddCurrencyReq = 2000903,
        GtoA_AdminAddCurrencyRes = 2000904,
        GtoA_AdminResetSeasonPassPremiumReq = 2000905,
        GtoA_AdminResetSeasonPassPremiumRes = 2000906,
        GtoA_AdminAddSeasonPassPointReq = 2000907,
        GtoA_AdminAddSeasonPassPointRes = 2000908,
        GtoA_AdminResetPilotRewardReq = 2000909,
        GtoA_AdminResetPilotRewardRes = 2000910,
        GtoA_AdminResetSeasonRewardReq = 2000911,
        GtoA_AdminResetSeasonRewardRes = 2000912,
        GtoA_AdminResetPlayerRewardReq = 2000913,
        GtoA_AdminResetPlayerRewardRes = 2000914,
        GtoA_AdminAddPilotExpReq = 2000915,
        GtoA_AdminAddPilotExpRes = 2000916,
        GtoA_AdminResetPilotExpLevelReq = 2000917,
        GtoA_AdminResetPilotExpLevelRes = 2000918,
        GtoA_APIAdminCompleteMissionReq = 2000919,
        GtoA_APIAdminCompleteMissionRes = 2000920,
        GtoA_APIAdminAddPilotReq = 2000921,
        GtoA_APIAdminAddPilotRes = 2000922,
        GtoA_APIAdminAddRobotReq = 2000923,
        GtoA_APIAdminAddRobotRes = 2000924,
        GtoA_APIAdminAddSkinReq = 2000925,
        GtoA_APIAdminAddSkinRes = 2000926,
        GtoA_APIAdminAddFrameReq = 2000927,
        GtoA_APIAdminAddFrameRes = 2000928,
        GtoA_APIAdminAddKillMarkerReq = 2000929,
        GtoA_APIAdminAddKillMarkerRes = 2000930,
        GtoA_APIAdminAddEmoticonReq = 2000931,
        GtoA_APIAdminAddEmoticonRes = 2000932,
        GtoA_APIAdminAddProfileIconReq = 2000933,
        GtoA_APIAdminAddProfileIconRes = 2000934,
        GtoA_APIAdminRemovePilotReq = 2000935,
        GtoA_APIAdminRemovePilotRes = 2000936,
        GtoA_APIAdminRemoveRobotReq = 2000937,
        GtoA_APIAdminRemoveRobotRes = 2000938,
        GtoA_APIAdminRemoveSkinReq = 2000939,
        GtoA_APIAdminRemoveSkinRes = 2000940,
        GtoA_APIAdminRemoveFrameReq = 2000941,
        GtoA_APIAdminRemoveFrameRes = 2000942,
        GtoA_APIAdminRemoveKillMarkerReq = 2000943,
        GtoA_APIAdminRemoveKillMarkerRes = 2000944,
        GtoA_APIAdminRemoveEmoticonReq = 2000945,
        GtoA_APIAdminRemoveEmoticonRes = 2000946,
        GtoA_APIAdminRemoveProfileIconReq = 2000947,
        GtoA_APIAdminRemoveProfileIconRes = 2000948,
        GtoA_APIAdminRewardWaitMissionReq = 2000949,
        GtoA_APIAdminRewardWaitMissionRes = 2000950,
        GtoA_APISetPilotLevelReq = 2000951,
        GtoA_APISetPilotLevelRes = 2000952,
        GtoA_APIResetEmailReq = 2000953,
        GtoA_APIResetEmailRes = 2000954,

        ////////////////////////////////////////////////////
        /// Match <->API
        ////////////////////////////////////////////////////
        MtoA_APIGameRoomEndReq = 3000001,
        MtoA_APIGameRoomEndRes = 3000002,
        MtoA_APIPlayerGameInfoReq = 3000003,
        MtoA_APIPlayerGameInfoRes = 3000004,
        MtoA_GetRoomSlotInfoReq = 3000005,
        MtoA_GetRoomSlotInfoRes = 3000006,

        ////////////////////////////////////////////////////
        /// Match <-> Game
        ////////////////////////////////////////////////////
        GtoM_ExpireSessionSig = 6000001,
        GtoM_CreateRoomReq = 6000002,
        GtoM_CreateRoomRes = 6000003,
        GtoM_JoinRoomReq = 6000004,
        GtoM_JoinRoomRes = 6000005,
        GtoM_LeaveRoomReq = 6000006,
        GtoM_LeaveRoomRes = 6000007,
        GtoM_ReadyRoomReq = 6000008,
        GtoM_ReadyRoomRes = 6000009,
        GtoM_StartRoomReq = 6000010,
        GtoM_StartRoomRes = 6000011,
        GtoM_NotiChangeRoomUnitSig = 6000012,
        GtoM_MoveRoomSlotReq = 6000014,
        GtoM_MoveRoomSlotRes = 6000015,
        GtoM_BanishRoomPlayerReq = 6000016,
        GtoM_BanishRoomPlayerReS = 6000017,
        GtoM_SetUpRoomAISettingSig = 6000018,
        GtoM_MoveSceneLocationSig = 6000019,
        GtoM_QueryInGameInfoReq = 6000021,
        GtoM_QueryInGameInfoRes = 6000022,
        GtoM_CompleteMatchmakingSig = 6001001,
        GtoM_FailMatchmakingSig = 6001002,
        GtoM_EndGamePlayerSig = 6001003,
        GtoM_EndGameRoomSig = 6001004,
        GtoM_RestartMatchmakingSig = 6001005,
        GtoM_FailStartingGameSig = 6001006,


        //System(game -> api)
        GtoA_APICheckSeasonInfoReq = 7000001,
        GtoA_APICheckSeasonInfoRes = 7000002,
        GtoA_APIRefreshReq = 7000003,
        GtoA_APIRefreshRes = 7000004,
        GtoA_APIRefreshDailyShopReq = 7000005,
        GtoA_APIRefreshDailyShopRes = 7000006,


        ////////////////////////////////////////////////////
        /// Match -> Game
        ////////////////////////////////////////////////////


        //ETC (변경하지마세요. 플렉스 람다가 쓰고 있습니다.)
        FtoM_CompleteFlexMatchingSig = 8000001,
        FtoM_FailFlexMatchingSig = 8000004,
        StoS_KeepAliveReq = 8000005,
        StoS_KeepAliveRes = 8000006,

        EOF = 99999999
    }
}