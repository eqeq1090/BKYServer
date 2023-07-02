using Dapper;
using MySqlConnector;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using BKServerBase.Logger;
using BKServerBase.Util;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.Model.Common;
using BKWebAPIComponent.Model.Entity;
using BKWebAPIComponent.Model.Entity.Master;
using BKWebAPIComponent.Model.Entity.PlayerShard;

namespace BKWebAPIComponent.Mapper
{
    public class DBQueryMapper
    {
        public enum QueryDef
        {
            InsertGlobalPlayer,
            GetEmail,
            ResetEmail,
            GetCurrentSeason,
            GetGlobalPlayers,
            GetGlobalPlayerByToken,
            GetGlobalPlayerByEmail,
            GetGlobalPlayerByID,
            RemoveGlobalPlayer,
            UpdateGlobalPlayerLogin,
            InsertShardPlayer,
            UpdatePlayerName,
            UpdateGlobalPlayerName,
            UpdatePlayerRegion,
            UpdatePlayerPilot,
            UpdatePlayerFrame,
            UpdatePlayerProfileIcon,
            UpdatePlayerKillMarker,
            UpdatePlayerRobot,
            UpdatePlayerTutorialStep,
            ProgressPlayerRewardStep,
            ResetPlayerRewardStep,
            GetShardPlayer,
            GetCurrencyAll,
            GetCurrency,
            UpdateCurrency,
            IncreaseCurrency,
            RechargeCurrency,
            InitCurrency,
            InsertRobot,
            GetRobots,
            GetRobotByMasterID,
            UpdateRobotSkin,
            RemoveRobot,
            InsertPilot,
            GetPilots,
            GetPilotByMasterID,
            UpdatePilotExp,
            ResetPilotExp,
            UpdatePilotLevel,
            UpdatePilotSkin,
            UpdatePilotFrame,
            UpdatePilotPose,
            UpdatePilotGameLog,
            UpdatePilotTrophy,
            UpdatePilotMMRAndGamePlayLog,
            RemovePilot,
            ProgressPilotRewardStep,
            ResetPilotReward,
            InsertFrame,
            GetFrames,
            GetFrameByMasterID,
            RemoveFrame,
            InsertKillMarker,
            GetKillMarkers,
            GetKillMarkerByMasterID,
            RemoveKillMarker,
            InsertEmoticon,
            GetEmoticons,
            GetEmoticonByMasterID,
            RemoveEmoticon,
            InsertProfileIcon,
            GetProfileIcons,
            GetProfileIconByMasterID,
            RemoveProfileIcon,
            InsertSkin,
            GetSkins,
            GetSkinByMasterID,
            RemoveSkin,
            GetGamePlayLog,
            GetGamePlayByGameMode,
            UpsertPlayerGamePlayLog,
            IncreaseSeasonPoint,
            RewardSeasonStepFree,
            RewardSeasonStepPremium,
            UpdateSeasonPassState,
            ResetSeasonPassPremium,
            ResetSeasonPassPoint,
            CompleteSeasonPassStep,
            GetSeasonPoint,
            InsertPurchaseHistory,
            GetPurchaseHistory,
            GetPurchaseHistoryAll,
            InsertPlayerBattleLog,
            InsertGlobalRoomBattleLog,
            GetGlobalRoomBattleLog,
            GetPlayerBattleLog,
            InsertfriendRequest,
            GetFriendList,
            GetFriendByPlayerID,
            GetFriendRequest,
            GetFriendRequestCount,
            RemoveFriendRecommend,
            GetFriendCount,
            RemoveFriendRequest,
            InsertFriend,
            RemoveFriend,
            GetFriendPlayerInfoList,
            GetFriendPlayerInfo,
            GetFriendPlayerInfoDetail,
            GetFriendRequestPlayerInfoList,
            GetFriendRequestList,
            GetFriendRecommendPlayerInfoList,
            GetFriendRecommendList,
            InsertFriendRecommend,
            SearchFriend,
            GetFriendPlayer,
            SearchGlobalPlayer,
            SearchFriendByTag,
            GetGlobalPlayerByTag,
            GetFriendPlayerByTag,
            GetMissions,
            GetMission,
            InsertMission,
            InsertMissions,
            UpdateMissionState,
            UpdateMissionConditionCount,
            UpdateMissionClientChecked,
            DeleteMission,
            DeleteMissionAll,
            InsertLoginHistory,
            GetEmailByUID,
            UpdateSeason,
            GetDailyShopSlot,
            UpdateDailyShopSlot,
        }

        internal abstract class IDBQueryInfo
        {
            public string QueryString { get; init; }
            public QueryType QueryType { get; init; }

            public IDBQueryInfo(string queryString, QueryType queryType)
            {
                QueryString = queryString;
                QueryType = queryType;
            }

            protected const string m_LastInsertIDQueryString = "SELECT LAST_INSERT_ID();";

            public virtual Task<IQueryResult> ExecuteDBQuery<T>(MySqlConnection connection, T input, MySqlTransaction? transaction = null)
                where T : class, new()
            {
                throw new NotImplementedException();
            }

            public virtual IQueryResult ExecuteDBQuerySync<T>(MySqlConnection connection, T input, MySqlTransaction? transaction = null)
                where T : class, new()
            {
                throw new NotImplementedException();
            }
        }

        internal class DBQueryInfo<T, U> : IDBQueryInfo
            where T : class, new()
            where U : class, new()
        {
            public DBQueryInfo(string queryString, QueryType queryType)
                : base(queryString, queryType)
            {

            }

            public override async Task<IQueryResult> ExecuteDBQuery<V>(MySqlConnection connection, V input, MySqlTransaction? transaction = null)
                where V : class
            {
                try
                {
                    if (input is not T inputValue)
                    {
                        return new QueryResultNoResult(ServiceErrorCode.QUERY_INPUT_ENTITY_NOT_MATCHED);
                    }

                    switch (QueryType)
                    {
                        case QueryType.SelectOne:
                            {
                                var result = await connection.QueryAsync<U>(QueryString, inputValue, transaction: transaction);
                                //NOTE 한개가 넘는 갯수가 리턴될 경우 잡아내기
                                if (result.Count() > 1)
                                {
                                    return new QueryResultSingle<U>(ServiceErrorCode.QUERY_RESULT_COUNT_MATCHED);
                                }
                                if (result.Count() == 0)
                                {
                                    return new QueryResultSingle<U>(ServiceErrorCode.QUERY_RESULT_NOT_FOUND);
                                }
                                var firstRow = result.First();
                                return new QueryResultSingle<U>(ServiceErrorCode.SUCCESS, firstRow);
                            }
                        case QueryType.SelectMany:
                            {
                                var result = await connection.QueryAsync<U>(QueryString, inputValue, transaction: transaction);
                                //NOTE 한개가 넘는 갯수가 리턴될 경우 잡아내기
                                if (result.Count() == 0)
                                {
                                    return new QueryResultMultiple<U>(ServiceErrorCode.SUCCESS, new List<U>());
                                }
                                var allRows = result.ToList();
                                return new QueryResultMultiple<U>(ServiceErrorCode.SUCCESS, allRows);
                            }
                        case QueryType.InsertAndReturnID:
                            {
                                int affectedRows = await connection.ExecuteAsync(QueryString, inputValue, transaction: transaction);
                                //NOTE 한개가 넘는 갯수가 리턴될 경우 잡아내기
                                if (affectedRows == 0)
                                {
                                    return new QueryResultNoResult(ServiceErrorCode.QUERY_UPDATE_ROWS_COUNT_INVALID);
                                }
                                var lastInsertID = await connection.ExecuteScalarAsync<long>(m_LastInsertIDQueryString);
                                return new QueryResultID(ServiceErrorCode.SUCCESS, lastInsertID);
                            }
                        case QueryType.InsertMultiple:
                            {
                                var type = inputValue.GetType();
                                if (!type.IsGenericType)
                                {
                                    return new QueryResultSingle<U>(ServiceErrorCode.QUERY_INPUT_IS_NOT_MULTIPLE_RECORD);
                                }
                                var genericType = type.GetGenericArguments()[0];
                                if (genericType != typeof(IEntity))
                                {
                                    return new QueryResultSingle<U>(ServiceErrorCode.QUERY_INPUT_ENTITY_NOT_MATCHED);
                                }
                                var convertedValue = inputValue as List<IEntity>;
                                if (convertedValue == null)
                                {
                                    return new QueryResultSingle<U>(ServiceErrorCode.QUERY_INPUT_ENTITY_NOT_MATCHED);
                                }
                                StringBuilder sb = new StringBuilder();
                                var inputString = string.Join(",", convertedValue.Select(x => x.GetBulkString()).ToList());
                                var alteredQueryString = string.Format(QueryString, inputString);
                                int affectedRows = await connection.ExecuteAsync(alteredQueryString, transaction: transaction);
                                //NOTE 한개가 넘는 갯수가 리턴될 경우 잡아내기
                                if (affectedRows == 0)
                                {
                                    return new QueryResultRowCount(ServiceErrorCode.QUERY_UPDATE_ROWS_COUNT_INVALID);
                                }
                                return new QueryResultRowCount(ServiceErrorCode.SUCCESS, affectedRows);
                            }
                        case QueryType.Insert:
                        case QueryType.Update:
                        case QueryType.Delete:
                            {
                                int affectedRows = await connection.ExecuteAsync(QueryString, inputValue, transaction: transaction);
                                //NOTE 한개가 넘는 갯수가 리턴될 경우 잡아내기
                                if (affectedRows == 0)
                                {
                                    return new QueryResultRowCount(ServiceErrorCode.QUERY_UPDATE_ROWS_COUNT_INVALID);
                                }
                                return new QueryResultRowCount(ServiceErrorCode.SUCCESS, affectedRows);
                            }
                        default:
                            return new QueryResultNoResult(ServiceErrorCode.QUERY_INVALID_OPERATION);
                    }
                }
                catch(Exception ex)
                {
                    CoreLog.Critical.LogError(ex);
                    return new QueryResultNoResult(ServiceErrorCode.QUERY_INVALID_OPERATION);
                }
            }

            public override IQueryResult ExecuteDBQuerySync<V>(MySqlConnection connection, V input, MySqlTransaction? transaction = null)
                where V: class
            {
                if (input is not T inputValue)
                {
                    return new QueryResultNoResult(ServiceErrorCode.QUERY_INPUT_ENTITY_NOT_MATCHED);
                }


                switch (QueryType)
                {
                    case QueryType.SelectOne:
                        {
                            var result = connection.Query<U>(QueryString, inputValue, transaction: transaction);
                            //NOTE 한개가 넘는 갯수가 리턴될 경우 잡아내기
                            if (result.Count() != 1)
                            {
                                return new QueryResultSingle<U>(ServiceErrorCode.QUERY_RESULT_COUNT_MATCHED);
                            }
                            var firstRow = result.First();
                            return new QueryResultSingle<U>(ServiceErrorCode.SUCCESS, firstRow);
                        }
                    case QueryType.SelectMany:
                        {
                            var result = connection.Query<U>(QueryString, inputValue, transaction: transaction);
                            //NOTE 한개가 넘는 갯수가 리턴될 경우 잡아내기
                            if (result.Count() == 0)
                            {
                                return new QueryResultMultiple<U>(ServiceErrorCode.SUCCESS, new List<U>());
                            }
                            var allRows = result.ToList();
                            return new QueryResultMultiple<U>(ServiceErrorCode.SUCCESS, allRows);
                        }
                    case QueryType.InsertAndReturnID:
                        {
                            int affectedRows = connection.Execute(QueryString, inputValue, transaction: transaction);
                            //NOTE 한개가 넘는 갯수가 리턴될 경우 잡아내기
                            if (affectedRows == 0)
                            {
                                return new QueryResultNoResult(ServiceErrorCode.QUERY_UPDATE_ROWS_COUNT_INVALID);
                            }
                            var lastInsertID = connection.ExecuteScalar<long>(m_LastInsertIDQueryString);
                            return new QueryResultID(ServiceErrorCode.SUCCESS, lastInsertID);
                        }
                    case QueryType.Insert:
                    case QueryType.Update:
                    case QueryType.Delete:
                        {
                            int affectedRows = connection.Execute(QueryString, inputValue, transaction: transaction);
                            //NOTE 한개가 넘는 갯수가 리턴될 경우 잡아내기
                            if (affectedRows == 0)
                            {
                                return new QueryResultRowCount(ServiceErrorCode.QUERY_UPDATE_ROWS_COUNT_INVALID);
                            }
                            return new QueryResultRowCount(ServiceErrorCode.SUCCESS, affectedRows);
                        }
                    default:
                        return new QueryResultNoResult(ServiceErrorCode.QUERY_INVALID_OPERATION);
                }
            }
        }

        private ImmutableDictionary<QueryDef, IDBQueryInfo> m_QueryTables;

        public DBQueryMapper()
        {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            var builder = ImmutableDictionary.CreateBuilder<QueryDef, IDBQueryInfo>();

            //Global Player
            builder.Add(QueryDef.InsertGlobalPlayer, new DBQueryInfo<GlobalPlayerEntity, IDEntity>(
                "INSERT INTO `vill_global_player` (`did`, `hive_player_id`, `access_token`, `shard_num`, `name`, `player_tag`, `email`) " +
                "VALUES (@DID, @HivePlayerID, @AccessToken, @ShardNum, @Name, @PlayerTag, @Email)", QueryType.InsertAndReturnID));
            builder.Add(QueryDef.GetGlobalPlayers, new DBQueryInfo<VoidEntity, GlobalPlayerEntity>(
                "SELECT * FROM `vill_global_player`", QueryType.SelectMany));
            builder.Add(QueryDef.GetGlobalPlayerByToken, new DBQueryInfo<GlobalPlayerEntity, GlobalPlayerEntity>(
                "SELECT * FROM `vill_global_player` WHERE `access_token` = @AccessToken", QueryType.SelectOne));
            builder.Add(QueryDef.GetGlobalPlayerByEmail, new DBQueryInfo<GlobalPlayerEntity, GlobalPlayerEntity>(
                "SELECT * FROM `vill_global_player` WHERE `email` = @Email", QueryType.SelectOne));
            builder.Add(QueryDef.GetGlobalPlayerByID, new DBQueryInfo<GlobalPlayerEntity, GlobalPlayerEntity>(
                "SELECT * FROM `vill_global_player` WHERE `player_uid` = @PlayerUID", QueryType.SelectOne));
            builder.Add(QueryDef.GetGlobalPlayerByTag, new DBQueryInfo<GlobalPlayerEntity, GlobalPlayerEntity>(
                "SELECT * FROM `vill_global_player` WHERE `player_tag` = @PlayerTag", QueryType.SelectOne));
            builder.Add(QueryDef.RemoveGlobalPlayer, new DBQueryInfo<PlayerUIDEntity, VoidEntity>(
                "DELETE FROM `vill_global_player` WHERE `player_uid` = @PlayerUID", QueryType.Delete));
            builder.Add(QueryDef.UpdateGlobalPlayerLogin, new DBQueryInfo<GlobalPlayerEntity, VoidEntity>(
                "UPDATE  `vill_global_player` SET `login_date` = CURRENT_TIMESTAMP WHERE `player_uid` = @PlayerUID", QueryType.Update));
            builder.Add(QueryDef.UpdateGlobalPlayerName, new DBQueryInfo<GlobalPlayerEntity, VoidEntity>(
                "UPDATE `vill_global_player` SET `name` = @Name WHERE `player_uid` = @PlayerUID;", QueryType.Update));
            builder.Add(QueryDef.GetEmail, new DBQueryInfo<EmailEntity, EmailEntity>(
                "SELECT * FROM `email_list` where `email` = @Email", QueryType.SelectOne));
            builder.Add(QueryDef.GetEmailByUID, new DBQueryInfo<PlayerUIDEntity, PlayerEmailEntity>(
                "SELECT `player_uid`, `email` FROM `vill_global_player` where `player_uid` = @PlayerUID", QueryType.SelectOne));
            builder.Add(QueryDef.ResetEmail, new DBQueryInfo<GlobalPlayerEntity, VoidEntity>(
                "UPDATE `vill_global_player` SET `email` = '' WHERE `player_uid` = @PlayerUID;", QueryType.Update));
            builder.Add(QueryDef.InsertLoginHistory, new DBQueryInfo<LoginHistoryEntity, VoidEntity>(
                "INSERT INTO  `login_history` (`player_uid`, `email`, `reg_date`, `behavior_type`, `group_num`) " +
                "VALUES (@PlayerUID, @Email, @RegDate, @BehaviorType, @GroupNum);", QueryType.Insert));

            //ShardPlayer            
            builder.Add(QueryDef.InsertShardPlayer, new DBQueryInfo<ShardPlayerEntity, VoidEntity>(
                "INSERT INTO `vill_players` (`player_uid`, `name`, `tutorial_step`, `player_tag`) " +
                "VALUES (@PlayerUID, @Name, @TutorialStep, @PlayerTag);", QueryType.Insert));
            builder.Add(QueryDef.UpdatePlayerName, new DBQueryInfo<ShardPlayerEntity, VoidEntity>(
                "UPDATE `vill_players` SET `name` = @Name WHERE `player_uid` = @PlayerUID;", QueryType.Update));
            builder.Add(QueryDef.UpdatePlayerRegion, new DBQueryInfo<ShardPlayerEntity, VoidEntity>(
                "UPDATE `vill_players` SET `region_code` = @RegionCode WHERE `player_uid` = @PlayerUID;", QueryType.Update));
            builder.Add(QueryDef.UpdatePlayerTutorialStep, new DBQueryInfo<ShardPlayerEntity, VoidEntity>(
                "UPDATE `vill_players` SET `tutorial_step` = @TutorialStep WHERE `player_uid` = @PlayerUID;", QueryType.Update));
            builder.Add(QueryDef.ProgressPlayerRewardStep, new DBQueryInfo<ShardPlayerEntity, VoidEntity>(
                "UPDATE `vill_players` SET `player_reward_step` = `player_reward_step`+ 1 " +
                "WHERE `player_uid` = @PlayerUID AND `player_reward_step` = @PlayerRewardStep;", QueryType.Update));
            builder.Add(QueryDef.ResetPlayerRewardStep, new DBQueryInfo<ShardPlayerEntity, VoidEntity>(
                "UPDATE `vill_players` SET `player_reward_step` = 0 " +
                "WHERE `player_uid` = @PlayerUID", QueryType.Update));
            builder.Add(QueryDef.GetShardPlayer, new DBQueryInfo<PlayerUIDEntity, ShardPlayerEntity>(
                "SELECT * FROM `vill_players` WHERE `player_uid` = @PlayerUID;", QueryType.SelectOne));

            m_QueryTables = builder.ToImmutable();
        }

        internal IDBQueryInfo? GetQueryInfo(QueryDef def)
        {
            if (m_QueryTables.TryGetValue(def, out var value) == false)
            {
                return null;
            }
            return value;
        }
    }
}
