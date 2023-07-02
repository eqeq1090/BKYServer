using Microsoft.AspNetCore.Mvc;
using Mysqlx.Session;
using Polly;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKDataLoader.MasterData;
using BKProtocol;
using BKProtocol.G2A;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.Common.ServiceFactory.Database;
using BKWebAPIComponent.Common.Util;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.Manager;
using BKWebAPIComponent.Redis;
using BKWebAPIComponent.Service.Global;
using BKWebAPIComponent.Service.Player;

namespace BKWebAPIComponent.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlayerController : ControllerBase
    {
        private readonly DBServiceFactoryManager m_FactoryManager;
        private readonly ILogger<PlayerController> m_Logger;
        private readonly PlayerManager m_PlayerManager;
        private readonly RedisDataService m_RedisDataService;


        public PlayerController(ILogger<PlayerController> logger, DBServiceFactoryManager factoryManager, PlayerManager playerManager, RedisDataService redisDataService)
        {
            m_Logger = logger;
            m_FactoryManager = factoryManager;
            m_PlayerManager = playerManager;
            m_RedisDataService = redisDataService;
        }

        [HttpPut("login")]
        public async Task<APILoginRes> Login(APILoginReq req)
        {
            var apiRes = new APILoginRes();
            //TODO 프로토콜 변수 변경 후 정상화
            var authResult = await m_PlayerManager.Login(req.accessToken, req.DID, req.HivePlayerID,req.email);
            if (authResult.IsSuccess() == false)
            {
                apiRes.errorCode = ErrorCodeConvert.Instance.GetMsgErrorCode(apiRes, authResult.GetErrorCode());
                return apiRes;
            }

            var playerInfoAllEntity = authResult.GetResult();

            var sessionKey = Guid.NewGuid().ToString();
            var result = await m_RedisDataService.AddSessionIDAsync(playerInfoAllEntity.ShardPlayer.PlayerUID, sessionKey);
            if (result is false)
            {
                apiRes.errorCode = MsgErrorCode.RedisErrorAddSessionID;
                return apiRes;
            }

            var presenceData = await m_RedisDataService.GetPresenceDataAsync(playerInfoAllEntity.ShardPlayer.PlayerUID);
            if (presenceData is not null)
            {
                var utcNow = DateTime.UtcNow;
                var diffTotalSec = (int)(utcNow - presenceData.LastDateTime).TotalSeconds;
                if (diffTotalSec >= BaseConsts.SessionTimeOutSec)
                {
                    presenceData = null;
                }
                else
                {
                    presenceData.LastDateTime = utcNow;
                }
            }

            if (presenceData is null)
            {
                presenceData = new PresenceData()
                {
                    PlayerUID = playerInfoAllEntity.ShardPlayer.PlayerUID,
                    Location = LocationType.Lobby,
                    LastDateTime = DateTime.UtcNow,
                };
            }

            result = await m_RedisDataService.AddPresenceDataAsync(playerInfoAllEntity.ShardPlayer.PlayerUID, presenceData);
            if (result is false)
            {
                apiRes.errorCode = MsgErrorCode.RedisErrorAddPresenceData;
                return apiRes;
            }

            apiRes.playerInfo = EntityToIMsg.ToPlayerInfo(playerInfoAllEntity);
            apiRes.sessionKey = sessionKey;
            apiRes.presenceData = presenceData;
            return apiRes;
        }

        [HttpPut("{playerUID}/changename")]
        public async Task<APIChangeNameRes> ChangeName(long playerUID, APIChangeNameReq req)
        {
            var apiRes = new APIChangeNameRes();
            var selectResult = await m_PlayerManager.ChangeName(playerUID, req.newName);
            
            if (!selectResult.IsSuccess())
            {
                apiRes.errorCode = ErrorCodeConvert.Instance.GetMsgErrorCode(apiRes, selectResult.GetErrorCode());
                return apiRes;
            }
            apiRes.changedName = req.newName;
            return apiRes;
        }

       [HttpPut("{playerUID}/logout")]
        public async Task<APILogoutRes> Logout(long playerUID, APILogoutReq req)
        {
            await m_RedisDataService.ExpirePresenceDataAsync(playerUID, TimeSpan.FromSeconds(BaseConsts.SessionTimeOutSec));
            await m_RedisDataService.RemoveSessionIDAsync(playerUID);

            var apiRes = new APILogoutRes();

            var playerService = await m_FactoryManager.GetPlayerDBServiceFactory()
                .GetPlayerShardService<PlayerService>(playerUID);

            var getEmailResult = await m_FactoryManager.GetMasterDBServiceFactory()
                       .GetMasterDBService<MasterDBBaseService>()
                       .GetEmailByUID(playerUID);
            if (getEmailResult.IsSuccess() is false)
            {
                apiRes.errorCode = ErrorCodeConvert.Instance.GetMsgErrorCode(apiRes, getEmailResult.GetErrorCode());
                return apiRes;
            }

            var email = getEmailResult.GetResult().Email;
            if (string.IsNullOrEmpty(email) is false)
            {
                var getEmailInfoResult = await m_FactoryManager.GetMasterDBServiceFactory()
                .GetMasterDBService<MasterDBBaseService>()
                .GetEmail(getEmailResult.GetResult().Email);

                if (!getEmailInfoResult.IsSuccess())
                {
                    apiRes.errorCode = ErrorCodeConvert.Instance.GetMsgErrorCode(apiRes, getEmailInfoResult.GetErrorCode());
                    return apiRes;
                }

                var insertResult = await m_FactoryManager.GetMasterDBServiceFactory()
                    .GetMasterDBService<MasterDBBaseService>()
                    .InsertLoginHistory(playerUID, getEmailResult.GetResult().Email, BehaviorType.Logout, (GroupNum)getEmailInfoResult.GetResult().GroupNum);
                if (insertResult.IsSuccess() is false)
                {
                    apiRes.errorCode = ErrorCodeConvert.Instance.GetMsgErrorCode(apiRes, insertResult.GetErrorCode());
                    return apiRes;
                }
            }

            apiRes.errorCode = MsgErrorCode.Success;
            return apiRes;
        }
    }
}