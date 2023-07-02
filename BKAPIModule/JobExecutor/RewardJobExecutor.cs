using org.apache.zookeeper;
using System.Data.Common;
using System.Transactions;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKProtocol;
using BKProtocol.Enum;
using BKWebAPIComponent.Common.DBSession;
using BKWebAPIComponent.Common.ServiceFactory.Database;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.JobExecutor.JobProcess;
using BKWebAPIComponent.Model.Entity.Composite;
using BKWebAPIComponent.Service.Player;

namespace BKWebAPIComponent.JobExecutor
{
    public class RewardJobExecutor : IDisposable
    {
        private readonly AtomicFlag m_Disposed = new AtomicFlag(false);
        private DBTransaction m_DBTransaction = null!;
        private List<(int groupMasterID, int itemMasterID, Dictionary<RewardType, IRewardJobProcess?> dict)> m_JobList = new();
        private DBServiceFactoryManager m_ServiceFactoryManager;
        private long m_PlayerUID;
        private JobExecutorState m_State = JobExecutorState.NotHandled;
        private bool m_ExternalTransaction = false;

        //NOTE RewardJobExecutor는 재사용을 절대로 검토하지 않는다.
        //1회성 시퀀셜 프로세스로 이루어지며 사용 완료후에는 반드시 폐기하는 것을 전제로 한다.
        public RewardJobExecutor(DBServiceFactoryManager serviceFactoryManager, long playerUID, bool canMigrate)
        {
            m_ServiceFactoryManager = serviceFactoryManager;
            m_PlayerUID = playerUID;
            CanMigrate = canMigrate;
        }

        public bool CanMigrate { get; }

        public async Task PrepareAsync(List<RewardEntity> rewards, DBTransaction? transaction)
        {
            if (transaction == null)
            {
                var playerBaseService = await m_ServiceFactoryManager
                    .GetPlayerDBServiceFactory()
                    .GetPlayerShardService<PlayerBaseService>(m_PlayerUID);

                m_DBTransaction = await playerBaseService.GetDBTransaction();
            }
            else
            {
                m_ExternalTransaction = true;
                m_DBTransaction = transaction;
            }

            foreach (var reward in rewards)
            {
                PrepareReward(reward);
            }
        }

        private void PrepareReward(RewardEntity rewardEntity, int itemMasterID = 0, int groupMasterID = 0)
        {
            var targetDictTuple = m_JobList.Where(x => x.groupMasterID == groupMasterID).FirstOrDefault();
            if (targetDictTuple == default)
            {
                //targetDictTuple = (groupMasterID, itemMasterID, new Dictionary<RewardType, IRewardJobProcess?>());
                //foreach(RewardType type in Enum.GetValues(typeof(RewardType)))
                //{
                //    targetDictTuple.dict.Add(type, null);
                //}
                //m_JobList.Add(targetDictTuple);
            }
            var targetDict = targetDictTuple.dict;
            switch (rewardEntity.Category)
            {
                default:
                    break;
            }
        }

        //NOTE 외부 호출을 엄금합니다. 반드시 delegate로 넘겨진 곳에서만 사용합니다.
        public void MigrateReward(int groupMasterID, ExchangedRewardUniqueObject exchangedObject, RewardEntity rewardEntity)
        {
            var targetDictTuple = m_JobList.Where(x => x.groupMasterID == groupMasterID).FirstOrDefault();
            if (targetDictTuple == default)
            {
                //ERROR
                return;
            }
            var targetDict = targetDictTuple.dict;
            switch (rewardEntity.Category)
            {
                default:
                    break;
            }
        }

        public async Task<Dictionary<int,RewardResultGroupEntity>?> GiveRewards(bool autoComplete = true)
        {
            if (m_State != JobExecutorState.NotHandled)
            {
                //ERROR
                return null;
            }
            m_State = JobExecutorState.Started;
            foreach (var dictTuple in m_JobList)
            {
                foreach (var job in dictTuple.dict)
                {
                    if (job.Value == null)
                    {
                        continue;  
                    }
                    if (await job.Value.Prepare() == false)
                    {
                        ContentsLog.Critical.LogError($"GiveRewards prepare job stopped by {job.ToString()}");
                        m_DBTransaction.Rollback();
                        return null;
                    }
                }
            }

            bool migrateOccured = false;
            foreach (var dictTuple in m_JobList)
            {
                foreach (var job in dictTuple.dict)
                {
                    if (job.Value == null)
                    {
                        continue;
                    }
                    migrateOccured |= job.Value.Migrate();
                }
            }

            if (migrateOccured)
            {
                foreach (var dictTuple in m_JobList)
                {
                    foreach (var job in dictTuple.dict)
                    {
                        if (job.Value == null)
                        {
                            continue;
                        }
                        if (await job.Value.Amend() == false)
                        {
                            ContentsLog.Critical.LogWarning($"GiveRewards migrateOccured Amend job stopped by {job.ToString()}");
                            m_DBTransaction.Rollback();
                            return null;
                        }
                    }
                }
            }

            foreach (var dictTuple in m_JobList)
            {
                foreach (var job in dictTuple.dict)
                {
                    if (job.Value == null)
                    {
                        continue;
                    }
                    if (await job.Value.Process() != ConstEnum.ServiceErrorCode.SUCCESS)
                    {
                        ContentsLog.Critical.LogWarning($"GiveRewards Process job stopped by {job.ToString()}");
                        m_DBTransaction.Rollback();
                        return null;
                    }
                }
            }


            foreach (var dictTuple in m_JobList)
            {
                foreach (var job in dictTuple.dict)
                {
                    if (job.Value == null)
                    {
                        continue;
                    }
                    if (await job.Value.Validate() == false)
                    {
                        ContentsLog.Critical.LogWarning($"GiveRewards Validate job stopped by {job.ToString()}");
                        m_DBTransaction.Rollback();
                        return null;
                    }
                }
            }

            var resultDict = new Dictionary<int,RewardResultGroupEntity>();
            foreach (var dictTuple in m_JobList)
            {
                var newResult = new RewardResultGroupEntity();
                newResult.itemMasterID = dictTuple.itemMasterID;
                resultDict.Add(dictTuple.groupMasterID, newResult);
                foreach (var job in dictTuple.dict)
                {
                    if (job.Value == null)
                    {
                        continue;
                    }
                    job.Value.CollectResult(ref newResult);
                }
            }
            m_State = JobExecutorState.QueryExecuted;
            if (autoComplete == true)
            {
                m_DBTransaction.Commit();
                m_State = JobExecutorState.Committed;
            }
            return resultDict;
        }

        public void Complete()
        {
            if (m_State != JobExecutorState.QueryExecuted)
            {
                //ERROR
                return;
            }
            m_DBTransaction.Commit();
            m_State = JobExecutorState.Committed;
        }

        public void Rollback()
        {
            if (m_State != JobExecutorState.QueryExecuted)
            {
                //ERROR
                return;
            }
            m_DBTransaction.Rollback();
            m_State = JobExecutorState.Rollbacked;
        }

        public void Clear()
        {
            m_DBTransaction.Dispose();
            //TODO 모든 job을 dispose 해줄 필요가 있다.
        }

        public DBTransaction GetDBTransaction()
        {
            return m_DBTransaction;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (m_Disposed.IsOn == true || disposing == false)
            {
                return;
            }
            if (m_Disposed.On() == false)
            {
                return;
            }

            if (m_ExternalTransaction == false)
            {
                m_DBTransaction.Dispose();
            }


            //TODO 개별로 다 dispose해주는게 안전
            m_JobList.Clear();
        }
    }
}
