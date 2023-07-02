using BKProtocol;
using BKProtocol.Enum;
using BKWebAPIComponent.Common.DBSession;
using BKWebAPIComponent.Common.ServiceFactory.Database;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.JobExecutor.JobInputContext;
using BKWebAPIComponent.JobExecutor.JobResultModel;
using BKWebAPIComponent.Model.Entity.Composite;

namespace BKWebAPIComponent.JobExecutor.JobProcess
{
    public abstract class IRewardJobProcess
    {
        protected DBTransaction m_DBTransaction;
        protected DBServiceFactoryManager m_ServiceFactoryManager;
        protected RewardType m_RewardType;
        protected RewardJobExecutor m_ParentExecutor;
        protected bool m_Executed = false;
        protected long m_PlayerUID;
        protected IRewardInputContext m_InputContext;
        protected IRewardResultModel m_ResultModel;
        protected int m_GroupMasterID;

        protected IRewardJobProcess(IRewardInputContext inputContext, 
            IRewardResultModel resultModel,
            RewardType rewardType, 
            long playerUID, 
            RewardJobExecutor parent, 
            DBServiceFactoryManager serviceFactoryManager, 
            DBTransaction transaction,
            int groupMasterID) 
        {
            m_ParentExecutor = parent;
            m_ServiceFactoryManager = serviceFactoryManager;
            m_DBTransaction = transaction;
            m_RewardType = rewardType;
            m_PlayerUID = playerUID;

            m_InputContext = inputContext;
            m_ResultModel = resultModel;
            m_GroupMasterID = groupMasterID;
        }

        public void AddReward(RewardEntity entity)
        {
            m_InputContext.AddReward(RewardOriginateType.FromOrigin, entity);
        }

        public virtual void BypassReward(ExchangedRewardUniqueObject exchangedObject, RewardEntity rewardEntity)
        {
            m_ResultModel.AcceptMigrate(exchangedObject);
            m_InputContext.AddReward(RewardOriginateType.Migrate, rewardEntity);
        }

        public override string ToString()
        {
            return $"RewardType : {m_RewardType}, GroupIndex : {m_GroupMasterID}";
        }

        public abstract Task<bool> Prepare();
        public abstract Task<bool> Amend();

        public bool Migrate()
        {
            return m_InputContext.ExecuteMigrate((exchangeRewardObject, entity) =>
            {
                m_ParentExecutor.MigrateReward(m_GroupMasterID, exchangeRewardObject, entity);
            });
        }

        public abstract Task<ServiceErrorCode> Process();

        public abstract void CollectResult(ref Model.Entity.Composite.RewardResultGroupEntity group);

        public abstract Task<bool> Validate();
    }
}
