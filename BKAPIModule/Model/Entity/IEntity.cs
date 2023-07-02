using BKServerBase.Logger;

namespace BKWebAPIComponent.Model.Entity
{
    public abstract class IEntity
    {
        public virtual string GetBulkString()
        {
            ContentsLog.Critical.LogError("IEntity bulkstring not implemented");
            return string.Empty;
        }
    }
}
