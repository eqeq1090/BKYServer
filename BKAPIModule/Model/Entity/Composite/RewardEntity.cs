using System.Diagnostics;
using BKProtocol;
using BKProtocol.Enum;

namespace BKWebAPIComponent.Model.Entity.Composite
{
    public abstract class RewardEntity : IEntity
    {
        public readonly RewardType Category;
        public  int Amount { get; private set; }

        public RewardEntity(RewardType category, int amount) 
        {
            Category = category;
            Amount = amount;
        }

        public virtual void Multiply(int magnitude)
        {
            Amount *= magnitude;
        }

        public void SetAmount(int amount)
        {
            Amount = amount;
        }
    }
}
