using static BKWebAPIComponent.Mapper.DBQueryMapper;
using BKWebAPIComponent.Mapper;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.Model.Common;
using BKServerBase.Util;
using BKWebAPIComponent.Model.Entity.Master;
using static org.apache.zookeeper.OpResult;
using BKWebAPIComponent.Model.Entity.PlayerShard;
using BKProtocol;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using BKDataLoader.MasterData;
using BKWebAPIComponent.Model.Entity;
using BKServerBase.ConstEnum;
using BKServerBase.Config;
using BKWebAPIComponent.Common.DBSession;

namespace BKWebAPIComponent.Service.Global
{
    public class MasterDBBaseService : IDBService
    {
        public async Task<ServiceResult<GlobalPlayerEntity>> GetGlobalCharacterByToken(string token)
        {
            using (var session = await GetDBSession())
            {
                return await GetGlobalCharacterByTokenInternal(session, token);
            }
        }

        public async Task<ServiceResult<GlobalPlayerEntity>> GetGlobalCharacterByTokenInternal(AbstractDBSession session, string token)
        {
            var getResult = await session.ExecuteQuery<GlobalPlayerEntity, GlobalPlayerEntity>(QueryDef.GetGlobalPlayerByToken, new GlobalPlayerEntity()
            {
                AccessToken = token
            });
            if (getResult.IsSuccess() == false)
            {
                return new ServiceResult<GlobalPlayerEntity>(ServiceErrorCode.GLOBAL_PLAYER_NOT_FOUND);
            }
            return new ServiceResult<GlobalPlayerEntity>(ServiceErrorCode.SUCCESS, getResult.GetResult());
        }

        public async Task<ServiceResult<GlobalPlayerEntity>> GetGlobalCharacterByEmail(string email)
        {
            using (var session = await GetDBSession())
            {
                return await GetGlobalCharacterByEmailInternal(session, email);
            }
        }

        public async Task<ServiceResult<GlobalPlayerEntity>> GetGlobalCharacterByEmailInternal(AbstractDBSession session, string email)
        {
            var getResult = await session.ExecuteQuery<GlobalPlayerEntity, GlobalPlayerEntity>(QueryDef.GetGlobalPlayerByEmail, new GlobalPlayerEntity()
            {
                Email = email
            });
            if (getResult.IsSuccess() == false)
            {
                return new ServiceResult<GlobalPlayerEntity>(ServiceErrorCode.GLOBAL_PLAYER_NOT_FOUND);
            }
            return new ServiceResult<GlobalPlayerEntity>(ServiceErrorCode.SUCCESS, getResult.GetResult());
        }

        public async Task<ServiceResult<GlobalPlayerEntity>> CreateGlobalPlayer(string token, string DID, long hivePlayerID, string tag, string email)
        {
            var maxPlayerShardSize = ConfigManager.Instance.DBConnectionConf!.Shards[BKSchemaType.bk_player_shard].ShardSize;

            using (var session = await GetDBSession())
            {
                var createValue = new GlobalPlayerEntity()
                {
                    AccessToken = token,
                    DID = DID,
                    HivePlayerID = hivePlayerID,
                    Name = ConstEnum.Consts.DEFAULT_NAME,
                    ShardNum = SimpleRandUtil.Instance.Next(maxPlayerShardSize),
                    PlayerTag = tag,
                    Email = email
                };
                var createResult = await session.ExecuteQuery<IDEntity, GlobalPlayerEntity>(QueryDef.InsertGlobalPlayer, createValue);
                if (createResult.IsSuccess() == false)
                {
                    return new ServiceResult<GlobalPlayerEntity>(createResult.GetErrorCode());
                }
                if (email != string.Empty)
                {
                    var getNewResult = await GetGlobalCharacterByEmailInternal(session, email);
                    if (getNewResult.IsSuccess() == false)
                    {
                        return new ServiceResult<GlobalPlayerEntity>(getNewResult.GetErrorCode());
                    }
                    return new ServiceResult<GlobalPlayerEntity>(ServiceErrorCode.SUCCESS, getNewResult.GetResult());
                }
                else
                {
                    var getNewResult = await GetGlobalCharacterByTokenInternal(session, token);
                    if (getNewResult.IsSuccess() == false)
                    {
                        return new ServiceResult<GlobalPlayerEntity>(getNewResult.GetErrorCode());
                    }
                    return new ServiceResult<GlobalPlayerEntity>(ServiceErrorCode.SUCCESS, getNewResult.GetResult());
                }
                
            }
        }

        public async Task<ServiceResult<VoidEntity>> UpdateGlobalPlayerLogin(long playerUID)
        {
            using (var session = await GetDBSession())
            {
                var updateResult = await session.ExecuteQuery<VoidEntity, GlobalPlayerEntity>(QueryDef.UpdateGlobalPlayerLogin, new GlobalPlayerEntity()
                {
                    PlayerUID = playerUID
                });
                if (updateResult.IsSuccess() == false)
                {
                    return new ServiceResult<VoidEntity>(ServiceErrorCode.GLOBAL_PLAYER_NOT_FOUND);
                }
                return new ServiceResult<VoidEntity>(ServiceErrorCode.SUCCESS);
            }
        }

        public async Task<ServiceResult<VoidEntity>> InsertLoginHistory(long playerUID, string email, BehaviorType type, GroupNum num)
        {
            using (var session = await GetDBSession())
            {
                return await InsertLoginHistoryInternal(session, playerUID, email, type, num);
            }
        }

        public async Task<ServiceResult<VoidEntity>> InsertLoginHistoryInternal(AbstractDBSession session, long playerUID, string email, BehaviorType type, GroupNum num)
        {
            var insertResult = await session.ExecuteQuery<VoidEntity, LoginHistoryEntity>(QueryDef.InsertLoginHistory, new LoginHistoryEntity()
            {
                PlayerUID = playerUID,
                Email = email,
                RegDate = DateTime.UtcNow,
                BehaviorType = (short)type,
                GroupNum = (short)num,
            });
            if (insertResult.IsSuccess() == false)
            {
                return new ServiceResult<VoidEntity>(insertResult.GetErrorCode());
            }
            return new ServiceResult<VoidEntity>(ServiceErrorCode.SUCCESS);
        }

        public async Task<ServiceResult<VoidEntity>> RemoveGlobalCharacter(long playerUID)
        {
            using (var session = await GetDBSession())
            {
                var createValue = new PlayerUIDEntity()
                {
                    PlayerUID = playerUID
                };
                var createResult = await session.ExecuteQuery<IDEntity, PlayerUIDEntity>(QueryDef.RemoveGlobalPlayer, createValue);
                if (createResult.IsSuccess() == false)
                {
                    return new ServiceResult<VoidEntity>(createResult.GetErrorCode());
                }
                return new ServiceResult<VoidEntity>(ServiceErrorCode.SUCCESS, new VoidEntity());
            }
        }

        public async Task<ServiceResult<EmailEntity>> GetEmail(string email)
        {
            using (var session = await GetDBSession())
            {
                return await GetEmailInternal(session, email);
            }
        }

        public async Task<ServiceResult<EmailEntity>> GetEmailInternal(AbstractDBSession session, string email)
        {
            var getResult = await session.ExecuteQuery<EmailEntity, EmailEntity>(QueryDef.GetEmail, new EmailEntity()
            {
                Email = email
            });
            if (getResult.IsSuccess() == false)
            {
                return new ServiceResult<EmailEntity>(getResult.GetErrorCode());
            }
            return new ServiceResult<EmailEntity>(ServiceErrorCode.SUCCESS, getResult.GetResult());
        }

        public async Task<ServiceResult<PlayerEmailEntity>> GetEmailByUID(long playerUID)
        {
            using (var session = await GetDBSession())
            {
                var getResult = await session.ExecuteQuery<PlayerEmailEntity, PlayerUIDEntity>(QueryDef.GetEmailByUID, new PlayerUIDEntity()
                {
                    PlayerUID = playerUID
                });
                if (getResult.IsSuccess() == false)
                {
                    return new ServiceResult<PlayerEmailEntity>(getResult.GetErrorCode());
                }
                return new ServiceResult<PlayerEmailEntity>(ServiceErrorCode.SUCCESS, getResult.GetResult());
            }
        }

        public async Task<ServiceResult<int>> GetShardNum(long playerUID)
        {
            using (var session = await GetDBSession())
            {
                var getPlayerResult = session.ExecuteQuerySync<GlobalPlayerEntity, GlobalPlayerEntity>(QueryDef.GetGlobalPlayerByID, new GlobalPlayerEntity()
                {
                    PlayerUID = playerUID
                });
                if (getPlayerResult.IsSuccess() == false)
                {
                    return new ServiceResult<int>(getPlayerResult.GetErrorCode());
                }
                return new ServiceResult<int>(ServiceErrorCode.SUCCESS, getPlayerResult.GetResult().ShardNum);
            }
        }
        public async Task<ServiceResult<int>> GetShardNumByTag(string playerTag)
        {
            using (var session = await GetDBSession())
            {
                var getPlayerResult = session.ExecuteQuerySync<GlobalPlayerEntity, PlayerTagEntity>(QueryDef.GetGlobalPlayerByTag, new PlayerTagEntity()
                {
                    PlayerTag = playerTag,
                });
                if (getPlayerResult.IsSuccess() == false)
                {
                    return new ServiceResult<int>(getPlayerResult.GetErrorCode());
                }
                return new ServiceResult<int>(ServiceErrorCode.SUCCESS, getPlayerResult.GetResult().ShardNum);
            }
        }

        public async Task<ServiceResult<GlobalPlayerEntity>> GetPlayerUIDByTag(string playerTag)
        {
            using (var session = await GetDBSession())
            {
                //태그로 아이디 검색
                var getResult = await session.ExecuteQuery<GlobalPlayerEntity, GlobalPlayerEntity>(QueryDef.GetGlobalPlayerByTag, new GlobalPlayerEntity()
                {
                    PlayerTag = playerTag
                });
                if (!getResult.IsSuccess())
                {
                    return new ServiceResult<GlobalPlayerEntity>(getResult.GetErrorCode());
                }

                return new ServiceResult<GlobalPlayerEntity>(ConstEnum.ServiceErrorCode.SUCCESS, getResult.GetResult());
            }
        }

        public async Task<ServiceResult<VoidEntity>> ResetEmail(long playerUID)
        {
            using (var session = await GetDBSession())
            {
                //태그로 아이디 검색
                var resetResult = await session.ExecuteQuery<VoidEntity, GlobalPlayerEntity>(QueryDef.ResetEmail, new GlobalPlayerEntity()
                {
                    PlayerUID = playerUID
                });
                if (!resetResult.IsSuccess())
                {
                    return new ServiceResult<VoidEntity>(resetResult.GetErrorCode());
                }

                return new ServiceResult<VoidEntity>(ConstEnum.ServiceErrorCode.SUCCESS);
            }
        }

        public async Task<ServiceResult<GlobalPlayerEntity>> GetGlobalPlayerByUID(long playerUID)
        {
            using (var session = await GetDBSession())
            {
                //태그로 아이디 검색
                var getResult = await session.ExecuteQuery<GlobalPlayerEntity, GlobalPlayerEntity>(QueryDef.GetGlobalPlayerByID, new GlobalPlayerEntity()
                {
                    PlayerUID = playerUID
                });
                if (!getResult.IsSuccess())
                {
                    return new ServiceResult<GlobalPlayerEntity>(getResult.GetErrorCode());
                }

                return new ServiceResult<GlobalPlayerEntity>(ConstEnum.ServiceErrorCode.SUCCESS, getResult.GetResult());
            }
        }
    }
}
