using BKServerBase.Synchorization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BKServerBase.Messaging.Detail
{
    public sealed class Future 
    {
        private readonly Signal resolveSignal = new Signal();
        private readonly Signal<Exception> resolveErrorSignal = new Signal<Exception>();

        public void Resolve()
        {
            resolveSignal.Set();
        }

        public void ResolveFail(Exception e)
        {
            resolveErrorSignal.Set(e);
        }
        
        public void Then(Action success)
        {
            resolveSignal.OnCompleted(success);
        }

        public void Then(Action success, Action<Exception> error)
        {
            resolveSignal.OnCompleted(success);
            resolveErrorSignal.OnCompleted(error);
        }
    }

    public sealed class Future<T> 
    {
        private readonly Signal<T> resolveSignal = new Signal<T>();
        private readonly Signal<Exception> resolveErrorSignal = new Signal<Exception>();

        public void Resolve(T result)
        {
            resolveSignal.Set(result);
        }

        public void ResolveFail(Exception e)
        {
            resolveErrorSignal.Set(e);
        }
        
        public void Then(Action<T> success)
        {
            resolveSignal.OnCompleted(success);
        }

        public void Then(Action<T> success, Action<Exception> error)
        {
            resolveSignal.OnCompleted(success);
            resolveErrorSignal.OnCompleted(error);
        }
    }
}
