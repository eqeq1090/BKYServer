using BKServerBase.Logger;
using BKServerBase.Util;
using BKDataLoader.MasterData;
using BKProtocol;
using BKProtocol.Enum;
using BKWebAPIComponent.Common.DBSession;
using BKWebAPIComponent.Common.ResultClass;
using BKWebAPIComponent.Common.ServiceFactory.Database;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.JobExecutor;
using BKWebAPIComponent.Model.Entity.Composite;

namespace BKWebAPIComponent.Manager
{
    public class RewardManager
    {
        private DBServiceFactoryManager m_ServiceFactoryManager;

        public RewardManager(DBServiceFactoryManager serviceFactoryManager)
        {
            m_ServiceFactoryManager = serviceFactoryManager;
        }

        public async Task<ServiceResult<Dictionary<int,RewardResultGroupEntity>>> GiveReward(long playerUID, List<RewardEntity> rewards, DBTransaction? transaction = null, bool canMigrate = true)
        {
            try
            {
                using var rewardJobExecutor = new RewardJobExecutor(m_ServiceFactoryManager, playerUID, canMigrate);
                await rewardJobExecutor.PrepareAsync(rewards, transaction);

                var result = await rewardJobExecutor.GiveRewards(transaction == null);
                if (result == null)
                {
                    //향후 giverewards에서 더 구체적인 실패 사유를 받도록 리팩토링할 수 있다.
                    return new ServiceResult<Dictionary<int, RewardResultGroupEntity>>(ServiceErrorCode.REWARDJOB_FAILED);
                }
                return new ServiceResult<Dictionary<int, RewardResultGroupEntity>>(ServiceErrorCode.SUCCESS, result);
            }
            catch(Exception ex)
            {
                CoreLog.Critical.LogError(ex);
                return new ServiceResult<Dictionary<int, RewardResultGroupEntity>>(ServiceErrorCode.REWARDJOB_FAILED);
            }
        }


        
    }
}
