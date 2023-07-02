using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc.Formatters;
using Org.BouncyCastle.Ocsp;
using System.Linq;using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BKServerBase.Config;
using BKServerBase.Extension;
using BKServerBase.Logger;
using BKServerBase.Util;
using BKDataLoader.MasterData;
using BKProtocol;
using BKProtocol.G2A;
using BKWebAPIComponent.Common.DBSession;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.Common.ServiceFactory.Database;
using BKWebAPIComponent.Common.Util;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.External;
using BKWebAPIComponent.External.Hive;
using BKWebAPIComponent.Model.Common;
using BKWebAPIComponent.Model.Entity.Composite;
using BKWebAPIComponent.Model.Entity.Composite.Result;
using BKWebAPIComponent.Model.Entity.Master;
using BKWebAPIComponent.Model.Entity.PlayerShard;
using BKWebAPIComponent.Service.Global;
using BKWebAPIComponent.Service.Player;
using static org.apache.zookeeper.OpResult;
using static BKWebAPIComponent.Mapper.DBQueryMapper;

namespace BKWebAPIComponent.Manager
{
    public class PlayerManager
    {
        private DBServiceFactoryManager m_ServiceFactoryManager;
        private RewardManager m_RewardManager;
        private ExternalHttpClientService m_ExternalHttpClientService;

        public PlayerManager(DBServiceFactoryManager serviceFactoryManager, RewardManager rewardManager, ExternalHttpClientService clientService)
        {
            m_ServiceFactoryManager = serviceFactoryManager;
            m_RewardManager = rewardManager;
            m_ExternalHttpClientService = clientService;
        }

        public async Task<ServiceResult<PlayerAllInfoEntity>> Login(string accessToken, string DID, long hivePlayerID, string email)
        {
            //TODO Server-to-Server 인증 기능을 추가하게 되면 여기에 보강한다.
            if (ConfigManager.Instance.APIServerConf?.CheckAuth == true)
            {
                var header = new Dictionary<string, string>()
                {
                    { Consts.HIVE_AUTH_HEADER, accessToken },
                };
                var externalAuthResult = await m_ExternalHttpClientService.Post<HiveAuthBody, HiveAuthRes>(Consts.HIVE_DISTRIBUTION_AUTH_URL, header, new HiveAuthBody()
                {
                    appid = string.Empty, // TODO 향후 AppCenter에 키값 등록하면 그거 가져다가 Const 선언해서 사용
                    did = DID,
                    player_id = hivePlayerID,
                    hive_certification_key = string.Empty // TODO 향후 AppCenter에 키값 등록하면 그거 가져다가 Const 선언해서 사용
                });
                if (externalAuthResult == null)
                {
                    ContentsLog.Critical.LogError($"HiveAuth request failed, accessToken: {accessToken}");
                    return new ServiceResult<PlayerAllInfoEntity>(ServiceErrorCode.HIVE_AUTH_FAILED);
                }
                if (externalAuthResult.result_code != 0)
                {
                    ContentsLog.Critical.LogError($"HiveAuth response failed, accessToken: {accessToken}, resultCode: {externalAuthResult.result_code}");
                    return new ServiceResult<PlayerAllInfoEntity>(ServiceErrorCode.HIVE_AUTH_FAILED);
                }
            }

            var getGlobalPlayerResult = await GetGlobalPlayerAsync(email, accessToken);
            if (getGlobalPlayerResult.IsSuccess() is false)
            {
                return new ServiceResult<PlayerAllInfoEntity>(getGlobalPlayerResult.GetErrorCode());
            }

            var playerUID = getGlobalPlayerResult.GetResult();

            //NOTE 계정이 안만들어졌으면 글로벌 계정 생성 -> 샤드 계정 생성 -> 계정 기본 항목 지급 루틴 적용-> 정보조회로 처리
            //이 단계에서는 아직 이름이 정해지지 않았음
            var newPlayerAllEntity = new PlayerAllInfoEntity();
            if (playerUID == 0)
            {
                var serviceResult = await CreatePlayerAsync(accessToken, DID, hivePlayerID, email);
                if (serviceResult.IsSuccess() is false)
                {
                    ContentsLog.Critical.LogError($"CreatePlayerAsync failed, errorCode: {serviceResult.GetErrorCode()}");
                    return new ServiceResult<PlayerAllInfoEntity>(serviceResult.GetErrorCode());
                }

                var result = serviceResult.GetResult();
                playerUID = result.PlayerUID;
                newPlayerAllEntity.ShardPlayer = result;
            }
            else
            {
                var serviceResult = await GetPlayerAsync(playerUID);
                if (serviceResult.IsSuccess() is false)
                {
                    ContentsLog.Critical.LogError($"GetPlayerAsync failed, errorCode: {serviceResult.GetErrorCode()}, playerUID: {playerUID}");
                    return new ServiceResult<PlayerAllInfoEntity>(serviceResult.GetErrorCode());
                }

                var result = serviceResult.GetResult();
                newPlayerAllEntity.ShardPlayer = result;
            }

            var serviceFactory = m_ServiceFactoryManager.GetPlayerDBServiceFactory();
            var playerService = await serviceFactory.GetPlayerShardService<PlayerService>(playerUID);
            using var session = await playerService.GetDBSession();

   

            //이메일 로그인 시 로그인 히스토리 남기기
            if (email != string.Empty)
            {
                var masterService = m_ServiceFactoryManager.GetMasterDBServiceFactory()
                    .GetMasterDBService<MasterDBBaseService>();

                using var globalSession = await masterService.GetDBSession();
                var getEmailResult = await masterService.GetEmailInternal(globalSession, email);
                if (getEmailResult.IsSuccess())
                {
                    var groupNum = (BKServerBase.ConstEnum.GroupNum)getEmailResult.GetResult().GroupNum;
                    var insertHistoryResult = await masterService
                        .InsertLoginHistoryInternal(globalSession, playerUID, email, BKServerBase.ConstEnum.BehaviorType.Login, groupNum);
                    if (insertHistoryResult.IsSuccess() is false)
                    {
                        ContentsLog.Critical.LogError($"InsertLoginHistoryInternal failed, playerUID: {playerUID}, email: {email}");
                    }
                    
                }
                else
                {
                    ContentsLog.Critical.LogError($"GetEmailInternal failed, email: {email}");
                }
            }

            //TODO
            //상점 관련 리프레시 및 조회
            //플레이 로그 조회
            return new ServiceResult<PlayerAllInfoEntity>(ConstEnum.ServiceErrorCode.SUCCESS, newPlayerAllEntity);
        }

     

        public async Task<ServiceResult<ChangeNameResultEntity>> ChangeName(long playerUID, string name)
        {
            var playerBaseService = await m_ServiceFactoryManager
                .GetPlayerDBServiceFactory()
                .GetPlayerShardService<PlayerBaseService>(playerUID);

            var globalService = m_ServiceFactoryManager
                .GetMasterDBServiceFactory()
                .GetMasterDBService<MasterDBBaseService>();

            using var baseTransaction = await playerBaseService.GetDBTransaction();
            using var globalTransaction = await globalService.GetDBTransaction();

            var inputValue = new ShardPlayerEntity()
            {
                PlayerUID = playerUID,
                Name = name
            };
            //playerdb 업데이트
            var updateResult = await baseTransaction.ExecuteQuery<VoidEntity, ShardPlayerEntity>(QueryDef.UpdatePlayerName, inputValue);
            if (updateResult.IsSuccess() == false)
            {
                baseTransaction.Rollback();
                globalTransaction.Rollback();
                return new ServiceResult<ChangeNameResultEntity>(updateResult.GetErrorCode());
            }
            var inputValue1 = new GlobalPlayerEntity()
            {
                PlayerUID = playerUID,
                Name = name
            };
            //global db 업데이트
            var updateGlobalResult = await globalTransaction.ExecuteQuery<VoidEntity, GlobalPlayerEntity>(QueryDef.UpdateGlobalPlayerName, inputValue1);

            if (updateGlobalResult.IsSuccess() == false)
            {
                baseTransaction.Rollback();
                globalTransaction.Rollback();
                return new ServiceResult<ChangeNameResultEntity>(updateGlobalResult.GetErrorCode());
            }
            //결과 가져오기
            var getResult = await playerBaseService.GetPlayerInternal(baseTransaction, playerUID);
            if (getResult.IsSuccess() == false)
            {
                baseTransaction.Rollback();
                globalTransaction.Rollback();
                return new ServiceResult<ChangeNameResultEntity>(getResult.GetErrorCode());
            }
            if (getResult.GetResult().Name != name)
            {
                baseTransaction.Rollback();
                globalTransaction.Rollback();
                return new ServiceResult<ChangeNameResultEntity>(ConstEnum.ServiceErrorCode.PLAYER_NAME_NOT_CHANGED);
            }
            var resultEntity = new ChangeNameResultEntity();
            resultEntity.ShardPlayerEntity = getResult.GetResult();
            baseTransaction.Commit();
            globalTransaction.Commit();
            return new ServiceResult<ChangeNameResultEntity>(ConstEnum.ServiceErrorCode.SUCCESS, resultEntity);
        }

        private async Task<ServiceResult<ShardPlayerEntity>> 
            GetPlayerAsync(long playerUID)
        {
            var playerService = await m_ServiceFactoryManager
                .GetPlayerDBServiceFactory()
                .GetPlayerShardService<PlayerService>(playerUID);

            using var transaction = await playerService.GetDBTransaction();

            var updateLoginResult = await m_ServiceFactoryManager.GetMasterDBServiceFactory()
                .GetMasterDBService<MasterDBBaseService>()
                .UpdateGlobalPlayerLogin(playerUID);
            if (!updateLoginResult.IsSuccess())
            {
                transaction.Rollback();
                return new ServiceResult<ShardPlayerEntity>(ServiceErrorCode.PLAYER_LOGIN_DATE_UPDATE_FAILED);
            }

            var getPlayerResult = await playerService
                .GetPlayer(playerUID);
            if (!getPlayerResult.IsSuccess())
            {
                transaction.Rollback();
                return new ServiceResult<ShardPlayerEntity>(getPlayerResult.GetErrorCode());
            }

            transaction.Commit();

            return new ServiceResult<ShardPlayerEntity>((getPlayerResult.GetResult()));
        }

        private async Task<ServiceResult<ShardPlayerEntity>> 
            CreatePlayerAsync(string accessToken, string DID, long hivePlayerID, string email)
        {
            if (email != string.Empty)
            {
                var getEmailResult = await m_ServiceFactoryManager.GetMasterDBServiceFactory()
                    .GetMasterDBService<MasterDBBaseService>()
                    .GetEmail(email);

                if (!getEmailResult.IsSuccess())
                {
                    return new ServiceResult<ShardPlayerEntity>(ServiceErrorCode.PLAYER_EMAIL_NOT_FOUND);
                }
            }

            //NOTE X-Transaction. masterDB + playerShardDB
            //TODO 인증 정보를 더 많이 넣어야 할 수 있다.
            string tag = SimpleRandUtil.GenerateDigits(8);

            var createResult = await m_ServiceFactoryManager.GetMasterDBServiceFactory()
                .GetMasterDBService<MasterDBBaseService>()
                .CreateGlobalPlayer(accessToken, DID, hivePlayerID, tag, email);
            if (!createResult.IsSuccess())
            {
                return new ServiceResult<ShardPlayerEntity>(createResult.GetErrorCode());
            }

            var playerUID = createResult.GetResult().PlayerUID;
            var playerService = await m_ServiceFactoryManager
                .GetPlayerDBServiceFactory()
                .GetPlayerShardService<PlayerService>(playerUID);

            using var transaction = await playerService.GetDBTransaction();

            var createShardPlayerResult = await playerService.CreatePlayerInternal(transaction, playerUID, tag);
            if (!createShardPlayerResult.IsSuccess())
            {
                transaction.Rollback();
                return new ServiceResult<ShardPlayerEntity>(createShardPlayerResult.GetErrorCode());
            }

            var getPlayerResult = await playerService.GetPlayerInternal(transaction, playerUID);
            if (!getPlayerResult.IsSuccess())
            {
                transaction.Rollback();
                return new ServiceResult<ShardPlayerEntity>(getPlayerResult.GetErrorCode());
            }

            transaction.Commit();

            return new ServiceResult<ShardPlayerEntity>(getPlayerResult.GetResult());
        }

        private async Task<ServiceResult<long>> GetGlobalPlayerAsync(string email, string accessToken)
        {
            if (string.IsNullOrEmpty(email) is false)
            {
                var getGlobalPlayerResult = await m_ServiceFactoryManager.GetMasterDBServiceFactory()
                    .GetMasterDBService<MasterDBBaseService>()
                    .GetGlobalCharacterByEmail(email);
                if (getGlobalPlayerResult.IsSuccess() is false)
                {
                    var errorCode = getGlobalPlayerResult.GetErrorCode();
                    if (errorCode is ServiceErrorCode.GLOBAL_PLAYER_NOT_FOUND)
                    {
                        return new ServiceResult<long>(ServiceErrorCode.SUCCESS, 0);
                    }

                    ContentsLog.Critical.LogError($"GetGlobalCharacterByEmail failed, errorCode: {errorCode}, email: {email}");
                    return new ServiceResult<long>(getGlobalPlayerResult.GetErrorCode());
                }

                var playerUID = getGlobalPlayerResult.GetResult().PlayerUID;
                return new ServiceResult<long>(ServiceErrorCode.SUCCESS, playerUID);
            }
            else if (string.IsNullOrEmpty(accessToken) is false)
            {
                var getGlobalPlayerResult = await m_ServiceFactoryManager.GetMasterDBServiceFactory()
                    .GetMasterDBService<MasterDBBaseService>()
                    .GetGlobalCharacterByToken(accessToken);
                if (getGlobalPlayerResult.IsSuccess() is false)
                {
                    var errorCode = getGlobalPlayerResult.GetErrorCode();
                    if (errorCode is ServiceErrorCode.GLOBAL_PLAYER_NOT_FOUND)
                    {
                        return new ServiceResult<long>(ServiceErrorCode.SUCCESS, 0);
                    }

                    ContentsLog.Critical.LogError($"GetGlobalCharacterByToken failed, errorCode: {errorCode}, accessToken: {accessToken}");
                    return new ServiceResult<long>(getGlobalPlayerResult.GetErrorCode());
                }

                var playerUID = getGlobalPlayerResult.GetResult().PlayerUID;
                return new ServiceResult<long>(ServiceErrorCode.SUCCESS, playerUID);
            }

            return new ServiceResult<long>(ServiceErrorCode.GLOBAL_PLAYER_NOT_FOUND, 0);
        }
    }
}
