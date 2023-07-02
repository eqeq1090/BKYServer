using BKServerBase.ConstEnum;
using BKWebAPIComponent.Mapper;
using BKWebAPIComponent.Service;
using BKWebAPIComponent.Service.Global;
using BKWebAPIComponent.Service.Initialize;

namespace BKWebAPIComponent.Common.ServiceFactory.Database.Factory
{
    public class MasterDBServiceFactory : IDBServiceFactory
    {
        public MasterDBServiceFactory(DBQueryMapper mapper, ShardNumCheckService shardNumCheckService)
            : base(mapper, shardNumCheckService)
        {

        }

        public T GetMasterDBService<T>()
           where T : MasterDBBaseService, new()
        {
            return GetMasterDBServiceInternal<T>();
        }
    }
}
