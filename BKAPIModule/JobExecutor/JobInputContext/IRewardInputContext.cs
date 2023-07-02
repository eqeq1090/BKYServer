using BKProtocol;
using BKProtocol.Enum;
using BKWebAPIComponent.Common.DBSession;
using BKWebAPIComponent.Common.ServiceFactory.Database;
using BKWebAPIComponent.Model.Entity.Composite;

namespace BKWebAPIComponent.JobExecutor.JobInputContext
{
    public delegate void MigrateExecuteHandler(ExchangedRewardUniqueObject exchangedReward, RewardEntity entity);
    public abstract class IRewardInputContext
    {
        protected long m_PlayerUID;
        protected DBTransaction m_DBTransaction;
        protected DBServiceFactoryManager m_ServiceFactoryManager;
        protected bool m_CanMigrate;

        protected List<(ExchangedRewardUniqueObject, RewardEntity)> m_MigrateTargets = new List<(ExchangedRewardUniqueObject, RewardEntity)>();

        //NOTE 훼손이 무섭다면 Entity 자체의 복사생성자부터 다 새로 만들어서 작업해야 한다.
        //접근 및 훼손이 의심되면 무조건 추가 구현할 것.
        public List<(RewardOriginateType, RewardEntity)> RewardEntities = new List<(RewardOriginateType, RewardEntity)>();

        

        public IRewardInputContext(long playerUID, DBTransaction transaction, DBServiceFactoryManager serviceFactoryManager, bool canMigrate)
        {
            m_DBTransaction = transaction;
            m_ServiceFactoryManager = serviceFactoryManager;
            m_PlayerUID = playerUID;
            m_CanMigrate = canMigrate;
        }

        public abstract bool AddReward(RewardOriginateType type, RewardEntity entity);

        public bool ExecuteMigrate(MigrateExecuteHandler handler)
        {
            if (m_MigrateTargets.Count == 0)
            {
                return false;
            }
            foreach (var item in m_MigrateTargets)
            {
                handler(item.Item1, item.Item2);
            }
            return true;
        }

        protected void AddMigrateTarget(ExchangedRewardUniqueObject exchangedRewardUniqueObject, RewardEntity rewardEntity)
        {
            m_MigrateTargets.Add((exchangedRewardUniqueObject, rewardEntity));
        }

        public abstract Task<bool> Prepare();

        public abstract Task<bool> Amend();
    }
}
