using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BKServerBase.Util;
using Prometheus;
using System.Collections.Concurrent;
using System.IO;
using System.Diagnostics;
using BKServerBase.Logger;
using System.Runtime.ExceptionServices;

namespace BKServerBase.Threading
{
    public class CustomTaskBase : ObjectCounterHelper<CustomTaskBase>
    {
        private static long m_UnresolvedTaskCount = 0;
        private static readonly Gauge m_PrometheusUnresolvedTask = Metrics
        .CreateGauge("customtask_unresolved_count", "Unresolved CustomTask Count");

        public static long UnresolvedTaskCount
        {
            get => Interlocked.Read(ref m_UnresolvedTaskCount);
        }
        private Action? m_continuation = null;
        public bool IsCompleted { get; private set; } = false;
        public ExceptionDispatchInfo? Exception { get; private set; } = null;

        private static ConcurrentDictionary<string, Gauge.Child> CustomTaskCounts = new ConcurrentDictionary<string, Gauge.Child>();
        private Gauge.Child m_PrometheusGauge;

        protected CustomTaskBase(string caller)
        {
            Interlocked.Increment(ref m_UnresolvedTaskCount);
            m_PrometheusUnresolvedTask.Set(m_UnresolvedTaskCount);
            m_PrometheusGauge = CustomTaskCounts.GetOrAdd(caller, Metrics.CreateGauge("customtask_creation", "CustomTaskCreation", "kind").WithLabels(caller));
            m_PrometheusGauge.Inc();
        }
        ~CustomTaskBase()
        {
            if (IsCompleted == false)
            {
                Interlocked.Decrement(ref m_UnresolvedTaskCount);
                m_PrometheusUnresolvedTask.Set(m_UnresolvedTaskCount);
                m_PrometheusGauge.Dec();
            }
        }
        internal void AddContinuation(Action action)
        {
            if (m_continuation != null)
            {
                throw new Exception("duplicate continuation");
            }
            m_continuation = action;
        }

        public void SetException(Exception e)
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException();
            }
            Exception = ExceptionDispatchInfo.Capture(e);
            Complete();
        }
        protected void Complete()
        {
            IsCompleted = true;
            Interlocked.Decrement(ref m_UnresolvedTaskCount);
            m_PrometheusUnresolvedTask.Set(m_UnresolvedTaskCount);

            try
            {
                if (Exception != null)
                { }
                m_continuation?.Invoke();
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogFatal($"CustomTaskInvokeException (Exception) {e} {e.StackTrace}");
            }
            finally
            {
                m_continuation = null;
            }
        }
    }
    public struct AsyncCustomTaskMethodBuilder<TResult>
    {
        public CustomTask<TResult> Task { get; }
        public AsyncCustomTaskMethodBuilder(string caller)
        {
            Task = new CustomTask<TResult>(caller);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public static AsyncCustomTaskMethodBuilder<TResult> Create()
        {
#if DEBUG
            StackTrace st = new StackTrace(true);
            var sf = st.GetFrame(1);
            var method = sf?.GetMethod();
            string caller = $"{method?.ReflectedType?.Name ?? ""}:{method?.Name}";
            return new AsyncCustomTaskMethodBuilder<TResult>(caller);
#else
            return new AsyncCustomTaskMethodBuilder<TResult>("");
#endif
        }

        public void SetException(Exception exception)
        {
            Task.SetException(exception);
        }

        public void SetResult(TResult result)
        {
            Task.SetResult(result);
        }
    }

    [AsyncMethodBuilder(typeof(AsyncCustomTaskMethodBuilder<>))]
    public class CustomTask<TResult> : CustomTaskBase
    {
        public static CustomTask<TResult> MakeDummyCustomTask(string caller, TResult result)
        {
            var dummyResult = new CustomTask<TResult>(caller);
            dummyResult.SetResult(result);
            return dummyResult;
        }

        public static CustomTask<TResult> FromResult(TResult result, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var task = new CustomTask<TResult>(TagBuilder.MakeTag(file, line));
            task.SetResult(result);

            return task;
        }

        public TResult Result { get; private set; }
        public void SetResult(TResult r)
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException();
            }
            Result = r;
            Complete();
        }

        public void ContinueWith(Action<Exception?, TResult> callback)
        {
            if (IsCompleted)
            {
                callback(Exception?.SourceException, Result);
                return;
            }

            AddContinuation(() =>
            {
                callback(Exception?.SourceException, Result);
            });
        }
        public CustomTaskAwaiter<TResult> GetAwaiter()
        {
            CustomTaskAwaiter<TResult> ta = new CustomTaskAwaiter<TResult>(this);
            return ta;
        }
#nullable disable
        public CustomTask(string caller)
        : base(caller)
        {

        }
#nullable restore
        public CustomTask(string caller, TResult result)
            : base(caller)
        {
            Result = result;
            Complete();
        }
    }

    public struct CustomTaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
    {
        CustomTask<TResult> m_task;
        public bool IsCompleted
        {
            get
            {
                return m_task.IsCompleted;
            }
        }

        public CustomTaskAwaiter(CustomTask<TResult> t)
        {
            m_task = t;
        }
        public TResult GetResult()
        {
            if (m_task.Exception != null) m_task.Exception.Throw();
            return m_task.Result;
        }

        public void OnCompleted(Action continuation)
        {
            if (m_task.IsCompleted)
            {
                continuation();
                return;
            }
            m_task.AddContinuation(continuation);
        }


        public void UnsafeOnCompleted(Action continuation)
        {
            if (m_task.IsCompleted)
            {
                continuation();
                return;
            }
            m_task.AddContinuation(continuation);
        }
    }

    public struct AsyncCustomTaskMethodBuilder
    {
        public CustomTask Task { get; }

        public AsyncCustomTaskMethodBuilder(string caller)
        {
            Task = new CustomTask(caller);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public static AsyncCustomTaskMethodBuilder Create()
        {
#if DEBUG
            StackTrace st = new StackTrace(true);
            var sf = st.GetFrame(1);
            var method = sf?.GetMethod();
            string caller = $"{method?.ReflectedType?.Name ?? ""}:{method?.Name}";
            return new AsyncCustomTaskMethodBuilder(caller);
#else
            return new AsyncCustomTaskMethodBuilder("");
#endif
        }

        public void SetException(Exception exception)
        {
            Task.SetException(exception);
        }

        public void SetResult()
        {
            Task.SetResult();
        }
    }

    [AsyncMethodBuilder(typeof(AsyncCustomTaskMethodBuilder))]
    public class CustomTask : CustomTaskBase
    {
        public CustomTask(string caller)
        : base(caller)
        {
        }

        public static CustomTask FromResult([CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var task = new CustomTask(TagBuilder.MakeTag(file, line));
            task.SetResult();

            return task;
        }

        public void SetResult()
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException();
            }
            Complete();
        }

        public void ContinueWith(Action<Exception?> callback)
        {
            if (IsCompleted)
            {
                callback(Exception?.SourceException);
                return;
            }
            AddContinuation(() =>
            {
                callback(Exception?.SourceException);
            });
        }

        public CustomTaskAwaiter GetAwaiter()
        {
            return new CustomTaskAwaiter(this);
        }
    }

    public struct CustomTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        CustomTask m_task;
        public bool IsCompleted
        {
            get
            {
                return m_task.IsCompleted;
            }
        }
        public CustomTaskAwaiter(CustomTask t)
        {
            m_task = t;
        }
        public void GetResult()
        {
            if (m_task.Exception != null) m_task.Exception.Throw();
        }

        public void OnCompleted(Action continuation)
        {
            if (m_task.IsCompleted)
            {
                continuation();
                return;
            }
            m_task.AddContinuation(continuation);
        }
        public void UnsafeOnCompleted(Action continuation)
        {
            if (m_task.IsCompleted)
            {
                continuation();
                return;
            }
            m_task.AddContinuation(continuation);
        }
    }

    public static class CustomTaskErrorHandler
    {
        public static void HandleError<TResult>(this CustomTask<TResult> task)
        {
            task.ContinueWith((ex, _) =>
           {
               if (ex != null)
               {
                   CoreLog.Critical.LogError(ex);
               }
           });
        }

        public static void HandleError(this CustomTask task)
        {
            task.ContinueWith((ex) =>
            {
                if (ex != null)
                {
                    CoreLog.Critical.LogError(ex);
                }
            });
        }
    }
}