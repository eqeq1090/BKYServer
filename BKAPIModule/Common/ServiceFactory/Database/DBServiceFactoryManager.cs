using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKWebAPIComponent.Common.ServiceFactory.Database.Factory;
using BKWebAPIComponent.Manager;
using BKWebAPIComponent.Mapper;
using BKWebAPIComponent.Service.Initialize;

namespace BKWebAPIComponent.Common.ServiceFactory.Database
{
    public class DBServiceFactoryManager
    {
        private DBQueryMapper m_DBQueryMapper;
        private PlayerDBServiceFactory m_PlayerDBServiceFactory;
        private MasterDBServiceFactory m_MasterDBServiceFactory;

        public DBServiceFactoryManager(DBQueryMapper dBQueryMapper, ShardNumCheckService shardNumCheckService)
        {
            m_DBQueryMapper = dBQueryMapper;
            m_PlayerDBServiceFactory = new PlayerDBServiceFactory(m_DBQueryMapper, shardNumCheckService);
            m_MasterDBServiceFactory = new MasterDBServiceFactory(m_DBQueryMapper, shardNumCheckService);
        }

        public PlayerDBServiceFactory GetPlayerDBServiceFactory()
        {
            return m_PlayerDBServiceFactory;
        }

        public MasterDBServiceFactory GetMasterDBServiceFactory()
        {
            return m_MasterDBServiceFactory;
        }
    }
}
