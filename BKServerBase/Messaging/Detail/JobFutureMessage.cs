using System.Diagnostics;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;

namespace BKServerBase.Messaging.Detail
{
    public sealed class JobFutureMessageWithResult<TResult> : IJobMessage
    {
        private readonly Func<TResult> action;
        private readonly Future<TResult> future;
        private readonly IActor futureOwner;

        public JobFutureMessageWithResult(Func<TResult> action, Future<TResult> future, IActor futureOwner, string tag)
        {
            this.action = action;
            this.future = future;
            this.futureOwner = futureOwner;
            Tag = tag;
        }   
        
        public bool IsAwait => false;
        public string Tag { get; }

        public void Execute()
        {
            try
            {
                TResult result = action.Invoke();
                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.Resolve(result);
                    });
                
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.ResolveFail(e);
                    });
            }
        }
    }

    public sealed class JobFutureMessage : IJobMessage
    {
        private readonly Action action;
        private readonly Future future;
        private readonly IActor futureOwner;

        public JobFutureMessage(Action action, Future future, IActor futureOwner, string tag)
        {
            this.action = action;
            this.future = future;
            this.futureOwner = futureOwner;
            Tag = tag;
        }

        public bool IsAwait => false;
        public string Tag { get; }

        public void Execute()
        {
            try
            {
                action.Invoke();
                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.Resolve();
                    });
                
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.ResolveFail(e);
                    });
            }
        }
    }

    public sealed class JobFutureMessage<TOwner> : IJobMessage
        where TOwner : class
    {
        private readonly IJobActor<TOwner> actor;
        private readonly Action<TOwner> action;
        private readonly Future future;
        private readonly IActor futureOwner;

        public JobFutureMessage(IJobActor<TOwner> actor, Action<TOwner> action, Future future, IActor futureOwner, string tag)
        {
            this.actor = actor;
            this.action = action;
            this.future = future;
            this.futureOwner = futureOwner;
            Tag = tag;
        }

        public bool IsAwait => false;
        public string Tag { get; }

        public void Execute()
        {
            try
            {
                actor.EnsureThreadSafe();

                action.Invoke(actor.Owner);
                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.Resolve();
                    });
                
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.ResolveFail(e);
                    });
                
            }
        }
    }

    public sealed class JobFutureMessageWithResult<TOwner, TResult> : IJobMessage
        where TOwner : class
    {
        private readonly IJobActor<TOwner> actor;
        private readonly Func<TOwner, TResult> action;
        private readonly Future<TResult> future;
        private readonly IActor futureOwner;

        public JobFutureMessageWithResult(IJobActor<TOwner> actor, Func<TOwner, TResult> action, Future<TResult> future, IActor futureOwner, string tag)
        {
            this.actor = actor;
            this.action = action;
            this.future = future;
            this.futureOwner = futureOwner;
            Tag = tag;
        }

        public bool IsAwait => false;
        public string Tag { get; }

        public void Execute()
        {
            try
            {
                actor.EnsureThreadSafe();

                TResult result = action.Invoke(actor.Owner);
                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.Resolve(result);
                    });

            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.ResolveFail(e);
                    });
            }
        }
    }

    public sealed class JobFutureAsyncMessageWithResult<TResult> : IJobMessage
    {
        private readonly JobDispatcher dispather;
        private readonly Func<CustomTask<TResult>> action;
        private readonly Future<TResult> future;
        private readonly IActor futureOwner;

        public JobFutureAsyncMessageWithResult(JobDispatcher dispatcher, Func<CustomTask<TResult>> action, Future<TResult> future, IActor futureOwner, string tag)
        {
            dispather = dispatcher;
            this.action = action;
            this.future = future;
            this.futureOwner = futureOwner;
            Tag = tag;
        }

        public bool IsAwait => true;
        public string Tag { get; }

        public async void Execute()
        {
            try
            {
                var prevTick = TimeUtil.GetCurrentTickMilliSec();
                TResult result = await action.Invoke();
                var elaspedTick = TimeUtil.GetCurrentTickDiffMilliSec(prevTick);

                if (elaspedTick > 500)
                {
                    throw new InvalidOperationException($"JobFutureAsyncMessageWithResult invalid logic elasped: {elaspedTick}, tag: {Tag}");
                }

                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.Resolve(result);
                    });
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.ResolveFail(e);
                    });
            }

            dispather.ContinueAfterAwait();
        }
    }

    public class JobFutureAsyncMessageWithResult<TOwner, TResult> : IJobMessage
        where TOwner : class
    {
        private readonly IJobActor<TOwner> actor;
        private readonly Func<TOwner, CustomTask<TResult>> action;
        private readonly Future<TResult> future;
        private readonly IActor futureOwner;

        public JobFutureAsyncMessageWithResult(IJobActor<TOwner> actor, Func<TOwner, CustomTask<TResult>> action, Future<TResult> future, IActor futureOwner, string tag)
        {
            this.actor = actor;
            this.action = action;
            this.future = future;
            this.futureOwner = futureOwner;
            Tag = tag;
        }

        public bool IsAwait => true;
        public string Tag { get; }

        public async void Execute()
        {
            try
            {
                actor.EnsureThreadSafe();

                var prevTick = TimeUtil.GetCurrentTickMilliSec();
                TResult result = await action.Invoke(actor.Owner);
                var elaspedTick = TimeUtil.GetCurrentTickDiffMilliSec(prevTick);

                if (elaspedTick > 500)
                {
                    throw new InvalidOperationException($"JobFutureAsyncMessageWithResult invalid logic elasped: {elaspedTick}, tag; {Tag}");
                }

                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.Resolve(result);
                    });
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                futureOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        future.ResolveFail(e);
                    });
            }

            actor.GetDispatcher()
                .ContinueAfterAwait();
        }
    }
}
