using System;
using System.Threading;
using BKServerBase.ConstEnum;

namespace BKServerBase.Interface
{
    public interface IRunnable : IDisposable
    {
        RunnableType RunnableType { get; }
        int ThreadWorkerID { get; }
        void OnUpdate();
        void OnPostUpdate();
        long GetID();
        int GetScore();
        void SetBlocked(bool flag);
        void SetThread(Thread thread, int threadWorkerID);
    }
}
