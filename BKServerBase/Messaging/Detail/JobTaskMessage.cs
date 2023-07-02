using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;

namespace BKServerBase.Messaging.Detail
{
    public class JobTaskMessage<TOwner> : IJobMessage
    where TOwner : class
    {
        private readonly IJobActor<TOwner> actor;
        private readonly Action<TOwner> action;
        private readonly CustomTask task;
        private readonly IActor taskOwner;

        public JobTaskMessage(IJobActor<TOwner> actor, Action<TOwner> action, CustomTask task, IActor taskOwner, string tag)
        {
            this.actor = actor;
            this.action = action;
            this.task = task;
            Tag = tag;
            this.taskOwner = taskOwner;
        }

        public bool IsAwait => false;
        public string Tag { get; private set; }

        public void Execute()
        {
            try
            {
                actor.EnsureThreadSafe();

                action.Invoke(actor.Owner);
                taskOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        task.SetResult();
                    });

            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                taskOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        task.SetException(e);
                    });
            }
        }
    }

    public class JobTaskMessageWithResult<TResult> : IJobMessage
    {
        private readonly Func<TResult> action;
        private readonly CustomTask<TResult> task;
        private readonly IActor taskOwner;

        public JobTaskMessageWithResult(Func<TResult> action, CustomTask<TResult> task, IActor taskOwner, string tag)
        {
            this.action = action;
            this.task = task;
            this.taskOwner = taskOwner;
            Tag = tag;
        }

        public bool IsAwait => false;
        public string Tag { get; }

        public void Execute()
        {
            try
            {
                TResult result = action.Invoke();
                taskOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        task.SetResult(result);
                    });

            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                taskOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        task.SetException(e);
                    });
            }
        }
    }

    public class JobTaskMessageWithResult<TOwner, TResult> : IJobMessage
        where TOwner : class
    {
        private readonly IJobActor<TOwner> actor;
        private readonly Func<TOwner, TResult> action;
        private readonly CustomTask<TResult> task;
        private readonly IActor taskOwner;

        public JobTaskMessageWithResult(IJobActor<TOwner> actor, Func<TOwner, TResult> action, CustomTask<TResult> task, IActor taskOwner, string tag)
        {
            this.actor = actor;
            this.action = action;
            this.task = task;
            Tag = tag;
            this.taskOwner = taskOwner;
        }

        public bool IsAwait => false;
        public string Tag { get; private set; }

        public void Execute()
        {
            try
            {
                actor.EnsureThreadSafe();

                TResult result = action.Invoke(actor.Owner);
                taskOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        task.SetResult(result);
                    });

            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                taskOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        task.SetException(e);
                    });
            }
        }
    }

    public class JobTaskAsyncMessageWithResult<TOwner, TResult> : IJobMessage
    where TOwner : class
    {
        private readonly IJobActor<TOwner> actor;
        private readonly Func<TOwner, CustomTask<TResult>> action;
        private readonly CustomTask<TResult> task;
        private readonly IActor taskOwner;

        public JobTaskAsyncMessageWithResult(IJobActor<TOwner> actor, Func<TOwner, CustomTask<TResult>> action, CustomTask<TResult> task, IActor taskOwner, string tag)
        {
            this.actor = actor;
            this.action = action;
            this.task = task;
            this.taskOwner = taskOwner;
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
                    throw new InvalidOperationException($"JobTaskAsyncMessage invalid logic elasped: {elaspedTick}, tag: {Tag}");
                }

                taskOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        task.SetResult(result);
                    });
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                taskOwner.GetDispatcher()
                    .Pend(() =>
                    {
                        task.SetException(e);
                    });
            }

            actor.GetDispatcher()
                .ContinueAfterAwait();
        }
    }
}
