using BKWebAPIComponent.Model.Entity.Composite;

namespace BKWebAPIComponent.JobExecutor.JobResultModel
{
    public abstract class IRewardResultModel
    {
        protected List<ExchangedRewardUniqueObject> ExchangedObjectList { get; set; } = new List<ExchangedRewardUniqueObject>();

        public virtual void CollectRewardResult(ref RewardResultGroupEntity group)
        {
            group.ExchangedObjectList.AddRange(ExchangedObjectList);
        }

        public void AcceptMigrate(ExchangedRewardUniqueObject exchangedObject)
        {
            ExchangedObjectList.Add(exchangedObject);
        }
    }
}
