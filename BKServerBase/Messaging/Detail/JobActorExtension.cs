using System.Runtime.CompilerServices;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;

namespace BKServerBase.Messaging.Detail
{
    public static class JobActorExtension
    {
        internal static void EnsureThreadSafe<TOwner>(this IJobActor<TOwner> actor)
            where TOwner : class
        {
            if (!actor.GetDispatcher().Test())
            {
                throw new Exception($"[IActor] invalid method call");
            }
        }

        public static void Post<TOwner>(this IJobActor<TOwner> actor, Action<TOwner> action, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            where TOwner : class
        {
            actor.GetDispatcher().Post(new JobMessage<TOwner>(
                actor: actor,
                action: action,
                tag: TagBuilder.MakeTag(file, line)));
        }

        public static void PostSync<TOwner>(this IJobActor<TOwner> actor, Func<TOwner, CustomTask> action, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            where TOwner : class
        {
            actor.GetDispatcher().Post(new JobMessageAsync<TOwner>(
                actor: actor,
                action: action,
                tag: TagBuilder.MakeTag(file, line)));
        }

        public static Future PostFuture<TOwner>(this IJobActor<TOwner> actor, Action<TOwner> action, IActor futureOwner, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            where TOwner : class
        {
            var future = new Future();

            actor.GetDispatcher().Post(new JobFutureMessage<TOwner>(
                actor: actor,
                action: action,
                future: future,
                futureOwner: futureOwner,
                tag: TagBuilder.MakeTag(file, line)));

            return future;
        }

        public static Future<TResult> PostFuture<TOwner, TResult>(this IJobActor<TOwner> actor, Func<TOwner, TResult> action, IActor futureOwner, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            where TOwner : class
        {
            var future = new Future<TResult>();

            actor.GetDispatcher().Post(new JobFutureMessageWithResult<TOwner, TResult>(
                actor: actor,
                action: action,
                future: future,
                futureOwner: futureOwner,
                tag: TagBuilder.MakeTag(file, line)));

            return future;
        }
        
        public static Future<TResult> PostFutureSync<TOwner, TResult>(this IJobActor<TOwner> actor, Func<TOwner, CustomTask<TResult>> action, IActor futureOwner, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            where TOwner : class
        {
            var future = new Future<TResult>();

            actor.GetDispatcher().Post(new JobFutureAsyncMessageWithResult<TOwner, TResult>(
                actor: actor,
                action: action,
                future: future,
                futureOwner: futureOwner,
                tag: TagBuilder.MakeTag(file, line)));

            return future;
        }

        public static CustomTask<TResult> PostTaskSync<TOwner, TResult>(this IJobActor<TOwner> actor, Func<TOwner, CustomTask<TResult>> action, IActor taskOwner, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            where TOwner : class
        {
            var task = new CustomTask<TResult>(TagBuilder.MakeTag(file, line));

            actor.GetDispatcher().Post(new JobTaskAsyncMessageWithResult<TOwner, TResult>(
                actor: actor,
                action: action,
                task: task,
                taskOwner: taskOwner,
                tag: TagBuilder.MakeTag(file, line)));
            

            return task;
        }

        public static CustomTask<TResult> PostTask<TOwner, TResult>(this IJobActor<TOwner> actor, Func<TOwner, TResult> action, IActor taskOwner, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            where TOwner : class
        {
            var task = new CustomTask<TResult>(TagBuilder.MakeTag(file, line));

            actor.GetDispatcher().Post(new JobTaskMessageWithResult<TOwner, TResult>(
                actor: actor,
                action: action,
                task: task,
                taskOwner: taskOwner,
                tag: TagBuilder.MakeTag(file, line)));


            return task;
        }

        public static CustomTask PostTask<TOwner>(this IJobActor<TOwner> actor, Action<TOwner> action, IActor taskOwner, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            where TOwner : class
        {
            var task = new CustomTask(TagBuilder.MakeTag(file, line));

            actor.GetDispatcher().Post(new JobTaskMessage<TOwner>(
                actor: actor,
                action: action,
                task: task,
                taskOwner: taskOwner,
                tag: TagBuilder.MakeTag(file, line)));


            return task;
        }
    }
}
