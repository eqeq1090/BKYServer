using BKServerBase.Logger;
using BKServerBase.Threading;
using BKProtocol;

namespace BKWebAPIComponent.ConstEnum
{
    public class ErrorCodeConvert : BaseSingleton<ErrorCodeConvert>
    {
        private static Dictionary<ServiceErrorCode, MsgErrorCode> m_ErrorCodeDict = new Dictionary<ServiceErrorCode, MsgErrorCode>();

        private ErrorCodeConvert()
        {
            m_ErrorCodeDict.Clear();
            m_ErrorCodeDict.Add(ServiceErrorCode.SUCCESS, MsgErrorCode.Success);
            m_ErrorCodeDict.Add(ServiceErrorCode.QUERY_NOT_FOUND, MsgErrorCode.QueryNotFound);
            m_ErrorCodeDict.Add(ServiceErrorCode.MISSION_GAME_DATA_NOT_FOUND, MsgErrorCode.MISSION_GAME_DATA_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.MISSION_DATA_SLOT_ALREADY_ALLOCATED, MsgErrorCode.MISSION_DATA_SLOT_ALREADY_ALLOCATED);
            m_ErrorCodeDict.Add(ServiceErrorCode.MISSION_STATE_CHANGE_INVALID, MsgErrorCode.MISSION_STATE_CHANGE_INVALID);
            m_ErrorCodeDict.Add(ServiceErrorCode.MISSION_CANT_COMPLETE_STATE, MsgErrorCode.MISSION_CANT_COMPLETE_STATE);
            m_ErrorCodeDict.Add(ServiceErrorCode.SEASON_GAME_DATA_NOT_FOUND, MsgErrorCode.SEASON_GAME_DATA_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SEASON_PASS_REWARD_STEP_NOT_MATCHED, MsgErrorCode.PLAYER_SEASON_PASS_REWARD_STEP_NOT_MATCHED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SEASON_PASS_REWARD_ITEMID_NOT_FOUND, MsgErrorCode.PLAYER_SEASON_PASS_REWARD_ITEMID_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SEASON_PASS_REWARD_STEP_COMPLETE, MsgErrorCode.PLAYER_SEASON_PASS_REWARD_STEP_COMPLETE);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SEASON_PASS_REWARD_STEP_UPDATE_FAILED, MsgErrorCode.PLAYER_SEASON_PASS_REWARD_STEP_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SEASON_PASS_ALREADY_STARTED, MsgErrorCode.PLAYER_SEASON_PASS_ALREADY_STARTED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_CURRENCY_NOT_FOUND, MsgErrorCode.PLAYER_CURRENCY_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_CURRENCY_NOT_ENOUGH, MsgErrorCode.PLAYER_CURRENCY_NOT_ENOUGH);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_NOT_FOUND, MsgErrorCode.PLAYER_PILOT_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_GAME_DATA_NOT_FOUND, MsgErrorCode.PLAYER_PILOT_GAME_DATA_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_SELECT_UPDATE_FAILED, MsgErrorCode.PLAYER_PILOT_SELECT_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_LEVEL_MAX, MsgErrorCode.PLAYER_PILOT_LEVEL_MAX);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_LEVEL_INVALID, MsgErrorCode.PLAYER_PILOT_LEVEL_INVALID);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_EXP_NOT_ENOUGH, MsgErrorCode.PLAYER_PILOT_EXP_NOT_ENOUGH);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_LEVEL_UPDATE_FAILED, MsgErrorCode.PLAYER_PILOT_LEVEL_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_SKIN_SAME, MsgErrorCode.PLAYER_PILOT_SKIN_SAME);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_SKIN_UPDATE_FAILED, MsgErrorCode.PLAYER_PILOT_SKIN_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_REWARD_STEP_INPUT_NOT_MATCHED, MsgErrorCode.PLAYER_PILOT_REWARD_STEP_INPUT_NOT_MATCHED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_TROPHY_REWARD_ITEMID_NOT_FOUND, MsgErrorCode.PLAYER_PILOT_TROPHY_REWARD_ITEMID_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_REWARD_STEP_COMPLETE, MsgErrorCode.PLAYER_PILOT_REWARD_STEP_COMPLETE);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PILOT_REWARD_STEP_UPDATE_FAILED, MsgErrorCode.PLAYER_PILOT_REWARD_STEP_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_EMOTICON_NOT_FOUND, MsgErrorCode.PLAYER_EMOTICON_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_EMOTICON_GAME_DATA_NOT_FOUND, MsgErrorCode.PLAYER_EMOTICON_GAME_DATA_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_EMOTICON_SLOT_UPDATE_FAILED, MsgErrorCode.PLAYER_EMOTICON_SLOT_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_EMOTION_SLOT_INVALID, MsgErrorCode.PLAYER_EMOTION_SLOT_INVALID);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_EMOTION_MANUAL_SLOT_NOT_ALLOWED_EMPTY, MsgErrorCode.PLAYER_EMOTION_MANUAL_SLOT_NOT_ALLOWED_EMPTY);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_CANT_REMOVE_EQUIPPED_EMOTICON, MsgErrorCode.PLAYER_CANT_REMOVE_EQUIPPED_EMOTICON);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRAME_NOT_FOUND, MsgErrorCode.PLAYER_FRAME_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRAME_GAME_DATA_NOT_FOUND, MsgErrorCode.PLAYER_FRAME_GAME_DATA_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRAME_SELECT_UPDATE_FAILED, MsgErrorCode.PLAYER_FRAME_SELECT_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_MARKER_NOT_FOUND, MsgErrorCode.PLAYER_MARKER_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_MARKER_GAME_DATA_NOT_FOUND, MsgErrorCode.PLAYER_MARKER_GAME_DATA_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_KILLMARKER_SELECT_UPDATE_FAILED, MsgErrorCode.PLAYER_KILLMARKER_SELECT_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PROFILE_ICON_NOT_FOUND, MsgErrorCode.PLAYER_PROFILE_ICON_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_PROFILE_ICON_SELECT_UPDATE_FAILED, MsgErrorCode.PLAYER_PROFILE_ICON_SELECT_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_ROBOT_NOT_FOUND, MsgErrorCode.PLAYER_ROBOT_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_ROBOT_GAME_DATA_NOT_FOUND, MsgErrorCode.PLAYER_ROBOT_GAME_DATA_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_ROBOT_SELECT_UPDATE_FAILED, MsgErrorCode.PLAYER_ROBOT_SELECT_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_ROBOT_SKIN_SAME, MsgErrorCode.PLAYER_ROBOT_SKIN_SAME);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_ROBOT_SKIN_UPDATE_FAILED, MsgErrorCode.PLAYER_ROBOT_SKIN_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SKIN_NOT_FOUND, MsgErrorCode.PLAYER_SKIN_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SKIN_GAME_DATA_NOT_FOUND, MsgErrorCode.PLAYER_SKIN_GAME_DATA_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SKIN_TARGET_UNIT_MASTERID_NOT_MATCHED, MsgErrorCode.PLAYER_SKIN_TARGET_UNIT_MASTERID_NOT_MATCHED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_NAME_NOT_CHANGED, MsgErrorCode.PLAYER_NAME_NOT_CHANGED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_USERINIT_NOT_FOUND, MsgErrorCode.PLAYER_USERINIT_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_LOGIN_DATE_UPDATE_FAILED, MsgErrorCode.PLAYER_LOGIN_DATE_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_TROPHY_REWARD_ITEMID_NOT_FOUND, MsgErrorCode.PLAYER_TROPHY_REWARD_ITEMID_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_REWARD_STEP_COMPLETE, MsgErrorCode.PLAYER_REWARD_STEP_COMPLETE);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_REWARD_STEP_UPDATE_FAILED, MsgErrorCode.PLAYER_REWARD_STEP_UPDATE_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_REWARD_STEP_INPUT_NOT_MATCHED, MsgErrorCode.PLAYER_REWARD_STEP_INPUT_NOT_MATCHED);
            m_ErrorCodeDict.Add(ServiceErrorCode.HIVE_AUTH_FAILED, MsgErrorCode.HIVE_AUTH_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.REWARDJOB_FAILED, MsgErrorCode.REWARDJOB_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_PLAYER_ALREADY_IN_TEAM, MsgErrorCode.TeamErrorAlreadyJoined);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_PLAYER_NOT_IN_TEAM, MsgErrorCode.TeamErrorNotBelongInTeam);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_PLAYER_INVALID, MsgErrorCode.TeamErrorInvalidID);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_INTERNAL, MsgErrorCode.TeamErrorInternal);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_TEAM_INVALID, MsgErrorCode.TeamErrorInvalid);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_FULL, MsgErrorCode.TeamErrorFullMembers);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_PLAYER_KICKED, MsgErrorCode.TeamErrorKickFailed);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_PLAYER_ALREADY_REQUESTED, MsgErrorCode.TeamErrorAlreayRequested);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_PLAYER_ALREADY_INVITED, MsgErrorCode.TeamErrorAlreadyInvited);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_MAX_INVITED, MsgErrorCode.TeamErrorMaxInvitation);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_MAX_REQUESTED, MsgErrorCode.TeamErrorMaxRequest);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_PLAYER_NOT_INVITED, MsgErrorCode.TeamErrorPlayerNotInvited);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_INVITER_NOT_IN_TEAM, MsgErrorCode.TeamErrorInviterNotBelongInTeam);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_TARGET_ALREADY_IN_TEAM, MsgErrorCode.TeamErrorAlreadyBelongInTeam);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_TARGET_KICKED, MsgErrorCode.TeamErrorMemberKicked);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_TARGET_ALREADY_INVITED, MsgErrorCode.TeamErrorAlreadyInvited);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_TARGET_ALREADY_JOIN_REQUESTED, MsgErrorCode.TeamErrorAlreadyJoinRequested);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_TARGET_NOT_INVITED, MsgErrorCode.TeamErrorNotInvited);
            m_ErrorCodeDict.Add(ServiceErrorCode.TEAM_PLAYER_DIDNT_REQUEST, MsgErrorCode.TeamErrorCantInvite);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SHOP_NOT_FOR_SALE_PERIOD, MsgErrorCode.PLAYER_SHOP_NOT_FOR_SALE_PERIOD);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SHOP_NOT_FOR_SALE_HIDDEN, MsgErrorCode.PLAYER_SHOP_NOT_FOR_SALE_HIDDEN);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SHOP_NOT_FOR_SALE_LIMIT, MsgErrorCode.PLAYER_SHOP_NOT_FOR_SALE_LIMIT);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SHOP_NOT_FOR_SALE_IAP_NOT_PERMITTED, MsgErrorCode.PLAYER_SHOP_NOT_FOR_SALE_IAP_NOT_PERMITTED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SHOP_FOR_SALE_IAP_ONLY, MsgErrorCode.PLAYER_SHOP_FOR_SALE_IAP_ONLY);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_SHOP_NOT_FOR_SALE_FREE, MsgErrorCode.PLAYER_SHOP_NOT_FOR_SALE_FREE);
            m_ErrorCodeDict.Add(ServiceErrorCode.ROOM_BATTLE_LOG_INSERT_FAILED, MsgErrorCode.ROOM_BATTLE_LOG_INSERT_FAILED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRIEND_ALREADY_FRIEND, MsgErrorCode.PLAYER_FRIEND_ALREADY_FRIEND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRIEND_ALREADY_REQUESTED_FRIEND, MsgErrorCode.PLAYER_FRIEND_ALREADY_REQUESTED_FRIEND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRIEND_ALREADY_GET_FRIEND_REQUEST, MsgErrorCode.PLAYER_FRIEND_ALREADY_GET_FRIEND_REQUEST);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRIEND_FULL_FRIEND_REQUESTED, MsgErrorCode.PLAYER_FRIEND_FULL_FRIEND_REQUESTED);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRIEND_REQUEST_FRIEND_NOT_AVAILABLE, MsgErrorCode.PLAYER_FRIEND_REQUEST_FRIEND_NOT_AVAILABLE);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRIEND_REQUEST_NOT_EXIST, MsgErrorCode.PLAYER_FRIEND_REQUEST_NOT_EXIST);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRIEND_FULL, MsgErrorCode.PLAYER_FRIEND_FULL);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRIEND_FULL_OPPONENT, MsgErrorCode.PLAYER_FRIEND_FULL_OPPONENT);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_FRIEND_NOT_FOUND, MsgErrorCode.PLAYER_FRIEND_NOT_FOUND);
            m_ErrorCodeDict.Add(ServiceErrorCode.PLAYER_EMAIL_NOT_FOUND, MsgErrorCode.PLAYER_EMAIL_NOT_FOUND);

            foreach (ServiceErrorCode errorCode in Enum.GetValues(typeof(ServiceErrorCode)))
            {
                if (m_ErrorCodeDict.ContainsKey(errorCode) == false)
                {
                    m_ErrorCodeDict.Add((ServiceErrorCode)errorCode, MsgErrorCode.ServiceCodeNotFound);
                }
            }
        }

        public MsgErrorCode GetMsgErrorCode(IAPIResMsg msg, ServiceErrorCode errorCode)
        {
            if (m_ErrorCodeDict.TryGetValue(errorCode, out MsgErrorCode result) == false)
            {
                return MsgErrorCode.InvalidErrorCode;
            }
            if (result != MsgErrorCode.Success)
            {
                ContentsLog.Critical.LogError($"<== msg : {msg.msgType.ToString()}'s Error [{errorCode}] Convert to [{result}]");
            }
            else
            {
                ContentsLog.Critical.LogError($"<== msg : {msg.msgType.ToString()}'s Error [{errorCode}] responsed");
            }
            return result;
        }
    }

    public enum ServiceErrorCode
    {
        SUCCESS,

        DB_CONNECTION_NOT_OPEN_STATE,
        DB_TRANSACTION_CURRUPTED,

        //Common
        //NOTE 쿼리맵퍼에서 지정된 쿼리를 찾을 수 없음.
        QUERY_NOT_FOUND,
        //NOTE 쿼리의 입력 인자의 수가 맞지 않음.
        QUERY_INPUT_ENTITY_NOT_MATCHED,
        QUERY_INPUT_IS_NOT_MULTIPLE_RECORD,
        QUERY_RESULT_NOT_FOUND,
        QUERY_RESULT_COUNT_MATCHED,
        QUERY_UPDATE_ROWS_COUNT_INVALID,
        QUERY_INVALID_OPERATION,
        QUERY_RESULT_TYPE_INVALID,

        REDIS_TRANSACTION_FAILED,
        REDIS_KEY_DOESNT_EXIST,
        REDIS_KEY_ALREADY_EXIST,
        REDIS_CREATED_KEY_NOT_FOUND,

        SERVICE_RESULT_TYPE_INVALID,
        MASTERDATA_NOT_FOUND,

        //GlobalCharacter
        GLOBAL_PLAYER_NOT_FOUND,
        
        //Player.Mission
        MISSION_GAME_DATA_NOT_FOUND,
        MISSION_DATA_SLOT_ALREADY_ALLOCATED,
        MISSION_STATE_CHANGE_INVALID,
        MISSION_CANT_COMPLETE_STATE,

        //Player.Season
        SEASON_GAME_DATA_NOT_FOUND,
        PLAYER_SEASON_PASS_REWARD_STEP_NOT_MATCHED,
        PLAYER_SEASON_PASS_REWARD_ITEMID_NOT_FOUND,
        PLAYER_SEASON_PASS_REWARD_STEP_COMPLETE,
        PLAYER_SEASON_PASS_REWARD_STEP_UPDATE_FAILED,
        PLAYER_SEASON_PASS_ALREADY_STARTED,

        //Player.Currency
        PLAYER_CURRENCY_NOT_FOUND,
        PLAYER_CURRENCY_NOT_ENOUGH,

        //Player.Pilot
        PLAYER_PILOT_NOT_FOUND,
        PLAYER_PILOT_GAME_DATA_NOT_FOUND,
        PLAYER_PILOT_SELECT_UPDATE_FAILED,
        PLAYER_PILOT_LEVEL_MAX,
        PLAYER_PILOT_LEVEL_INVALID,
        PLAYER_PILOT_EXP_NOT_ENOUGH,
        PLAYER_PILOT_LEVEL_UPDATE_FAILED,
        PLAYER_PILOT_SKIN_SAME,
        PLAYER_PILOT_SKIN_UPDATE_FAILED,
        PLAYER_PILOT_REWARD_STEP_INPUT_NOT_MATCHED,
        PLAYER_PILOT_TROPHY_REWARD_ITEMID_NOT_FOUND,
        PLAYER_PILOT_REWARD_STEP_COMPLETE,
        PLAYER_PILOT_REWARD_STEP_UPDATE_FAILED,

        //Player.Emoticon
        PLAYER_EMOTICON_NOT_FOUND,
        PLAYER_EMOTICON_GAME_DATA_NOT_FOUND,
        PLAYER_EMOTICON_SLOT_UPDATE_FAILED,
        PLAYER_EMOTION_SLOT_INVALID,
        PLAYER_EMOTION_MANUAL_SLOT_NOT_ALLOWED_EMPTY,
        PLAYER_CANT_REMOVE_EQUIPPED_EMOTICON,

        //Player.Frame
        PLAYER_FRAME_NOT_FOUND,
        PLAYER_FRAME_GAME_DATA_NOT_FOUND,
        PLAYER_FRAME_SELECT_UPDATE_FAILED,

        //Player.KillMarker
        PLAYER_MARKER_NOT_FOUND,
        PLAYER_MARKER_GAME_DATA_NOT_FOUND,
        PLAYER_KILLMARKER_SELECT_UPDATE_FAILED,

        //Player.ProfileIcon
        PLAYER_PROFILE_ICON_NOT_FOUND,
        PLAYER_PROFILE_ICON_SELECT_UPDATE_FAILED,

        //Player.Robot
        PLAYER_ROBOT_NOT_FOUND,
        PLAYER_ROBOT_GAME_DATA_NOT_FOUND,
        PLAYER_ROBOT_SELECT_UPDATE_FAILED,
        PLAYER_ROBOT_SKIN_SAME,
        PLAYER_ROBOT_SKIN_UPDATE_FAILED,

        //Player.Skin
        PLAYER_SKIN_NOT_FOUND,
        PLAYER_SKIN_GAME_DATA_NOT_FOUND,
        PLAYER_SKIN_TARGET_UNIT_MASTERID_NOT_MATCHED,

        //Player.Common
        PLAYER_NAME_NOT_CHANGED,
        PLAYER_USERINIT_NOT_FOUND,
        PLAYER_LOGIN_DATE_UPDATE_FAILED,
        PLAYER_TROPHY_REWARD_ITEMID_NOT_FOUND,
        PLAYER_REWARD_STEP_COMPLETE,
        PLAYER_REWARD_STEP_UPDATE_FAILED,
        PLAYER_REWARD_STEP_INPUT_NOT_MATCHED,
        PLAYER_EMAIL_NOT_FOUND,

        //HIVE
        HIVE_AUTH_FAILED,

        //RewardManager
        REWARDJOB_FAILED,

        // Teams
        TEAM_PLAYER_ALREADY_IN_TEAM,
        TEAM_PLAYER_NOT_IN_TEAM,
        TEAM_PLAYER_INVALID,
        TEAM_INTERNAL,
        TEAM_TEAM_INVALID,
        TEAM_FULL,
        TEAM_PLAYER_KICKED,
        TEAM_PLAYER_ALREADY_REQUESTED,
        TEAM_PLAYER_ALREADY_INVITED,
        TEAM_MAX_INVITED,
        TEAM_MAX_REQUESTED,
        TEAM_PLAYER_NOT_INVITED,
        TEAM_INVITER_NOT_IN_TEAM,
        TEAM_TARGET_ALREADY_IN_TEAM,
        TEAM_TARGET_KICKED,
        TEAM_TARGET_ALREADY_INVITED,
        TEAM_TARGET_ALREADY_JOIN_REQUESTED,
        TEAM_TARGET_NOT_INVITED,
        TEAM_PLAYER_DIDNT_REQUEST,

        //Shop
        PLAYER_SHOP_NOT_FOR_SALE_PERIOD,
        PLAYER_SHOP_NOT_FOR_SALE_HIDDEN,
        PLAYER_SHOP_NOT_FOR_SALE_LIMIT,
        PLAYER_SHOP_NOT_FOR_SALE_IAP_NOT_PERMITTED,
        PLAYER_SHOP_FOR_SALE_IAP_ONLY,
        PLAYER_SHOP_NOT_FOR_SALE_FREE,

        // BattleLog
        ROOM_BATTLE_LOG_INSERT_FAILED,
        //Player.Friend
        PLAYER_FRIEND_ALREADY_FRIEND,
        PLAYER_FRIEND_ALREADY_REQUESTED_FRIEND,
        PLAYER_FRIEND_ALREADY_GET_FRIEND_REQUEST,
        PLAYER_FRIEND_FULL_FRIEND_REQUESTED,
        PLAYER_FRIEND_REQUEST_FRIEND_NOT_AVAILABLE,

        PLAYER_FRIEND_REQUEST_NOT_EXIST,
        PLAYER_FRIEND_FULL,
        PLAYER_FRIEND_FULL_OPPONENT,

        PLAYER_FRIEND_NOT_FOUND,


        //NOTE 모든 컨텐츠는 각각의 서비스 에러코드를 가집니다.
        //에러코드는 ProtocolErrorCode로 변환되어 클라이언트에게 전송됩니다.
        //변환의 이유는 ServiceErrorCcode 중 Client에게 추상화해야 하는 에러코드가 존재하기 때문입니다.
    }
}
