using System.Threading;
using BKServerBase.ConstEnum;
using BKServerBase.Util;

namespace BKServerBase.Util
{
    public static class ThreadUtil
    {
        public static void SleepwithThreadSpinWait(int msecTimeout)
        {
            double startTick = TimeUtil.GetCurrentTickMilliSecDetail();
            while (true)
            {
                double lastTick = TimeUtil.GetCurrentTickMilliSecDetail();
                double elapsedTick = lastTick - startTick;
                if (elapsedTick >= msecTimeout)
                {
                    return;
                }
                Thread.SpinWait(BaseConsts.SAMPLING_TICK_COUNT_FOR_SPINWAIT);
            }
        }
    }
}
