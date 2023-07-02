using Amazon.S3.Model;
using BKProtocol;
using BKProtocol.Enum;
using BKWebAPIComponent.Model.Entity.PlayerShard;

namespace BKWebAPIComponent.Model.Entity.Composite
{
    public class RewardResultGroupEntity
    {
        public int itemMasterID { get; set; }
        public List<ExchangedRewardUniqueObject> ExchangedObjectList = new List<ExchangedRewardUniqueObject>();
        //TODO 보상에 대한 전체 결과
    }
    public class ExchangedRewardUniqueObject
    {
        public RewardType RewardType { get; set; }
        public int MasterID { get; set; }
        public int Amount { get; set; }
    }
}
