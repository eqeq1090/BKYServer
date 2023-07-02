using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;

namespace BKServerBase.Messaging.Detail
{
    public class JobMessage : IJobMessage
    {
        private Action action;

        public JobMessage(Action action, string tag)
        {
            this.action = action;
            Tag = tag;
        }

        public bool IsAwait => false;
        public string Tag { get; private set; }

        public void Execute()
        {
            try
            {
                action.Invoke();
            } 
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
            }
        }
    }

    public class JobMessageAsync : IJobMessage
    {
        private readonly JobDispatcher dispatcher;
        private readonly Func<CustomTask> action;

        public JobMessageAsync(JobDispatcher dispatcher, Func<CustomTask> action, string tag)
        {
            this.dispatcher = dispatcher;
            this.action = action;
            Tag = tag;
        }

        public bool IsAwait => true;

        public string Tag { get; private set; }

        public async void Execute()
        {
            try
            {
                var prevTick = TimeUtil.GetCurrentTickMilliSec();
                await action.Invoke();
                var elaspedTick = TimeUtil.GetCurrentTickDiffMilliSec(prevTick);

                if (elaspedTick > 500)
                {
                    throw new InvalidOperationException($"JobMessageAsync invalid logic elasped: {elaspedTick}, tag: {Tag}");
                }
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
            }

            dispatcher.ContinueAfterAwait();
        }
    }

    public class JobMessage<TOwner> : IJobMessage
        where TOwner : class
    {
        private IJobActor<TOwner> actor;
        private Action<TOwner> action;

        public JobMessage(IJobActor<TOwner> actor, Action<TOwner> action, string tag)
        {
            this.actor = actor;
            this.action = action;
            Tag = tag;
        }

        public bool IsAwait => false;
        public string Tag { get; private set; }

        public void Execute()
        {
            try
            {
                actor.EnsureThreadSafe();

                action.Invoke(actor.Owner);
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
            }
        }
    }

    public class JobMessageAsync<TOwner> : IJobMessage
        where TOwner : class
    {
        private IJobActor<TOwner> actor;
        private Func<TOwner, CustomTask> action;

        public JobMessageAsync(IJobActor<TOwner> actor, Func<TOwner, CustomTask> action, string tag)
        {
            this.actor = actor;
            this.action = action;
            Tag = tag;
        }

        public bool IsAwait => true;
        public string Tag { get; private set; }

        public async void Execute()
        {
            try
            {
                actor.EnsureThreadSafe();

                var prevTick = TimeUtil.GetCurrentTickMilliSec();
                await action.Invoke(actor.Owner);
                var elaspedTick = TimeUtil.GetCurrentTickDiffMilliSec(prevTick);

                if (elaspedTick > 500)
                {
                    throw new InvalidOperationException($"JobMessageAsync invalid logic elasped: {elaspedTick}, tag: {Tag}");
                }
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
            }

            actor.GetDispatcher()
                .ContinueAfterAwait();
        }
    }
}
