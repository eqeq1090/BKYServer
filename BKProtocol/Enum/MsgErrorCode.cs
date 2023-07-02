namespace BKProtocol
{
    public enum MsgErrorCode : int
    {
        Success = 0,
        InvalidErrorCode = 1,

        ContentErrorAddPlayer = 100,
        ContentErrorStartMatchmkaing = 101,
        ContentErrorStopMatchmaking = 102,
        ContentErrorInvalidMatchStatusType = 103,
        ContentErrorGameRoomCreation = 104,
        ContentErrorInvalidPilotMasterID = 105,
        ContentErrorUpdatePilotMMR = 106,
        ContentErrorJoinMember = 107,
        ContentErrorEmptyRoom = 108,
        ContentErrorEmptyFriendlyRoomInfo = 110,
        ContentErrorReadyRoomFailed = 111,
        ContentErrorRoomCodeNotExist = 112,
        ContentErrorAlreadyJoinedRoom = 113,
        ContentErrorFullRoomSlot = 114,
        ContentErrorNotFullRoomSlot = 115,
        ContentErrorPlayerNotReady = 116,
        ContentErrorSetUpTeamFailed = 117,
        ContentErrorNotLeader = 118,
        ContentErrorNotRoomMember = 119,
        ContentErrorEmptyRoomSlot = 120,
        ContentErrorSameRoomSlotIndex = 121,
        ContentErrorGetRoomSlotInfo = 122,
        ContentErrorAlreadyRoomSlotExist = 123,
        ContentErrorCannotMoveSceneLocation= 124,
        ContentErrorMatchmakingComplete = 125,
        ContentErrorBelongRoomWaitingSlot = 126,
        ContentErrorSetRoomSlotInfo = 127,
        ContentErrorAlreadyGameStarted = 128,
        ContentErrorRoomExpired = 129,
        ContentErrorGameIsNotRunning = 130,
        ContentErrorInvalidMatchType = 131,
        ContentErrorInsufficientTeamMember = 132,
        ContentErrorGetPilotFullInfoFailed = 133,
        ContentErrorLeaderCantMoveWaitingSlot = 134,
        ContentErrorRoomStarting = 135,
        ContentErrorPlayerReady = 136,
        ContentErrorGetRobotInfo = 137,
        ContentErrorGetPlayerActor = 138,
        ContentErrorGetTeamMemberInfo = 139,

        PlayerTeamNotFound = 200,
        PlayerTeamAlreadyCreated = 201,
        
        PlayerSessionDuplicated = 301,

        ApiErrorUrlNotFound = 1001,
        ApiErrorCantReadResponse = 1008,
        ApiErrorExceptionOccurred = 1009,
        ApiErrorMatchmakingFailed = 1010,
        ApiErrorMatchmakingTimeout = 1011,
        ApiErrorDB = 1012,
        ApiErrorNotDefined = 1013,
        ApiErrorGiveRewardFailed = 1014,
        ApiErrorSendFailed = 1015,
        ApiErrorNotStatusOk = 1016,
        ApiErrorInvalidSessionID = 1017,
        ApiErrorSessionExpired = 1018,

        ClientErrorSendTimeout = 1100,
        ClientErrorDuplicatedTimer = 1101,
        ClientErrorSendFailed = 1102,

        InvalidAdminCommandArgument = 2001,

        RedisErrorAddSessionID = 3001,
        RedisErrorAddPresenceData = 3002,
        RedisErrorGetPresenceDataMap = 3003,
        RedisErrorPublishFailed = 3004,
        RedisErrorSubscribeFailed = 3005,
        RedisErrorUnsubscribeFailed = 3006,
        RedisErrorAddFriendlyRoomData = 3007,
        RedisErrorGetFriendlyRoomData = 3008,
        RedisErrorSaveData = 3009,
        RedisErrorSessionExpired = 3010,

        NetworkErrorSendFailed = 4001,
        NetworkErrorSendMatchNode = 4002,

        CoreErrorGetPlayerMatchServerID = 5001,
        CoreErrorStartPhotonFailed = 5002,
        CoreErrorPickServerNoedFailed = 5003,
        CoreErrorQuantumCodeVersionNotCorrect = 5004,
        CoreErrorSessionEmpty = 5005,

        QueryNotFound = 6001,
        ServiceCodeNotFound = 99999999,

        //Contents

        //Contents.Friend
        //TODO: bokim1009 숫자 앞자리 분리
        //Player.Mission
        MISSION_GAME_DATA_NOT_FOUND = 1000001,
        MISSION_DATA_SLOT_ALREADY_ALLOCATED = 1000002,
        MISSION_STATE_CHANGE_INVALID = 1000003,
        MISSION_CANT_COMPLETE_STATE = 1000004,

        //Player.Season
        SEASON_GAME_DATA_NOT_FOUND = 1001001,
        PLAYER_SEASON_PASS_REWARD_STEP_NOT_MATCHED = 1001002,
        PLAYER_SEASON_PASS_REWARD_ITEMID_NOT_FOUND = 1001003,
        PLAYER_SEASON_PASS_REWARD_STEP_COMPLETE = 1001004,
        PLAYER_SEASON_PASS_REWARD_STEP_UPDATE_FAILED = 1001005,
        PLAYER_SEASON_PASS_ALREADY_STARTED = 1001006,

        //Player.Currency
        PLAYER_CURRENCY_NOT_FOUND = 1002001,
        PLAYER_CURRENCY_NOT_ENOUGH = 1002002,

        //Player.Pilot
        PLAYER_PILOT_NOT_FOUND = 1003001,
        PLAYER_PILOT_GAME_DATA_NOT_FOUND = 1003002,
        PLAYER_PILOT_SELECT_UPDATE_FAILED = 1003003,
        PLAYER_PILOT_LEVEL_MAX = 1003004,
        PLAYER_PILOT_LEVEL_INVALID = 1003005,
        PLAYER_PILOT_EXP_NOT_ENOUGH = 1003006,
        PLAYER_PILOT_LEVEL_UPDATE_FAILED = 1003007,
        PLAYER_PILOT_SKIN_SAME = 1003008,
        PLAYER_PILOT_SKIN_UPDATE_FAILED = 1003009,
        PLAYER_PILOT_REWARD_STEP_INPUT_NOT_MATCHED = 1003010,
        PLAYER_PILOT_TROPHY_REWARD_ITEMID_NOT_FOUND = 1003011,
        PLAYER_PILOT_REWARD_STEP_COMPLETE = 1003012,
        PLAYER_PILOT_REWARD_STEP_UPDATE_FAILED = 1003013,

        //Player.Emoticon
        PLAYER_EMOTICON_NOT_FOUND = 1004001,
        PLAYER_EMOTICON_GAME_DATA_NOT_FOUND = 1004002,
        PLAYER_EMOTICON_SLOT_UPDATE_FAILED = 1004003,
        PLAYER_EMOTION_SLOT_INVALID = 1004004,
        PLAYER_EMOTION_MANUAL_SLOT_NOT_ALLOWED_EMPTY = 1004005,
        PLAYER_CANT_REMOVE_EQUIPPED_EMOTICON = 1004006,

        //Player.Frame
        PLAYER_FRAME_NOT_FOUND = 1005001,
        PLAYER_FRAME_GAME_DATA_NOT_FOUND = 1005002,
        PLAYER_FRAME_SELECT_UPDATE_FAILED = 1005003,

        //Player.KillMarker
        PLAYER_MARKER_NOT_FOUND = 1006001,
        PLAYER_MARKER_GAME_DATA_NOT_FOUND = 1006002,
        PLAYER_KILLMARKER_SELECT_UPDATE_FAILED = 1006003,

        //Player.ProfileIcon
        PLAYER_PROFILE_ICON_NOT_FOUND = 1007001,
        PLAYER_PROFILE_ICON_SELECT_UPDATE_FAILED = 1007002,

        //Player.Robot
        PLAYER_ROBOT_NOT_FOUND = 1008001,
        PLAYER_ROBOT_GAME_DATA_NOT_FOUND = 1008002,
        PLAYER_ROBOT_SELECT_UPDATE_FAILED = 1008003,
        PLAYER_ROBOT_SKIN_SAME = 1008004,
        PLAYER_ROBOT_SKIN_UPDATE_FAILED = 1008005,

        //Player.Skin
        PLAYER_SKIN_NOT_FOUND = 1009001,
        PLAYER_SKIN_GAME_DATA_NOT_FOUND = 1009002,
        PLAYER_SKIN_TARGET_UNIT_MASTERID_NOT_MATCHED = 1009003,

        //Player.Common
        PLAYER_NAME_NOT_CHANGED = 1010001,
        PLAYER_USERINIT_NOT_FOUND = 1010002,
        PLAYER_LOGIN_DATE_UPDATE_FAILED = 1010003,
        PLAYER_TROPHY_REWARD_ITEMID_NOT_FOUND = 1010004,
        PLAYER_REWARD_STEP_COMPLETE = 1010005,
        PLAYER_REWARD_STEP_UPDATE_FAILED = 1010006,
        PLAYER_REWARD_STEP_INPUT_NOT_MATCHED = 1010007,
        PLAYER_EMAIL_NOT_FOUND = 1001008,

        //HIVE
        HIVE_AUTH_FAILED = 2000001,

        //RewardManager
        REWARDJOB_FAILED = 1011001,

        // Teams
        TeamErrorAlreadyJoined = 1012001,
        TeamErrorNotBelongInTeam = 1012002,
        TeamErrorInvalidID = 1012003,
        TeamErrorInternal = 1012004,
        TeamErrorInvalid = 1012005,
        TeamErrorFullMembers = 1012006,
        TeamErrorKickFailed = 1012007,
        TeamErrorAlreayRequested = 1012008,
        TeamErrorAlreadyInvited = 1012009,
        TeamErrorMaxInvitation = 1012010,
        TeamErrorMaxRequest = 1012011,
        TeamErrorPlayerNotInvited = 1012012,
        TeamErrorInviterNotBelongInTeam = 1012013,
        TeamErrorAlreadyBelongInTeam = 1012014,
        TeamErrorMemberKicked = 1012015,
        TeamErrorAlreadyJoinRequested = 1012017,
        TeamErrorNotInvited = 1012018,
        TeamErrorCantInvite = 1012019,
        TeamErrorNotFoundTeam = 1012020,
        TeamErrorNotLoaded = 1012021,
        TeamErrorNotLeader = 1012022,
        TeamErrorAddTeamJoinRequest = 1012023,
        TeamErrorAddJoinRequestMember = 1012024,
        TeamErrorLackOfTeamCode = 1012025,
        TeamErrorLeaderEmpty = 1012026,
        TeamErrorPlayerInfoEmpty = 1012027,
        TeamErrorJoinRequestEmpty = 1012028,
        TeamErrorNotMember = 1012029,
        TeamErrorPlayerInfoExpired = 1012030,
        TeamErrorNotBelongInJoinRequest = 1012031,

        //Shop
        PLAYER_SHOP_NOT_FOR_SALE_PERIOD = 1013001,
        PLAYER_SHOP_NOT_FOR_SALE_HIDDEN = 1013002,
        PLAYER_SHOP_NOT_FOR_SALE_LIMIT = 1013003,
        PLAYER_SHOP_NOT_FOR_SALE_IAP_NOT_PERMITTED = 1013004,
        PLAYER_SHOP_FOR_SALE_IAP_ONLY = 1013005,
        PLAYER_SHOP_NOT_FOR_SALE_FREE = 1013006,

        // BattleLog
        ROOM_BATTLE_LOG_INSERT_FAILED = 1014001,
        
        //Player.Friend
        PLAYER_FRIEND_ALREADY_FRIEND = 1015001,
        PLAYER_FRIEND_ALREADY_REQUESTED_FRIEND = 1015002,
        PLAYER_FRIEND_ALREADY_GET_FRIEND_REQUEST = 1015003,
        PLAYER_FRIEND_FULL_FRIEND_REQUESTED = 1015004,
        PLAYER_FRIEND_REQUEST_FRIEND_NOT_AVAILABLE = 1015005,
        PLAYER_FRIEND_REQUEST_NOT_EXIST = 1015006,
        PLAYER_FRIEND_FULL = 1015007,
        PLAYER_FRIEND_FULL_OPPONENT = 1015008,
        PLAYER_FRIEND_NOT_FOUND = 1015009,

        //Friend chat
        ChatFriendNotFriend = 1016001,

        //Season
        SeasonNotChanged = 1017001,
    }
}
