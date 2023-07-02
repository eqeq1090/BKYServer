using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BKServerBase.Interface;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKServerBase.Util;

namespace BKServerBase.Messaging
{
    [DebuggerDisplay("id = {id} count = {jobCount}")]
    public sealed class JobDispatcher : IDisposable
    {
        private static AsyncLocal<JobDispatcher?> asyncLocalDispatcher = new AsyncLocal<JobDispatcher?>();
        private static volatile int idSeed;
        private readonly ConcurrentQueue<IJobMessage> jobQueue = new ConcurrentQueue<IJobMessage>();
        private readonly ICommandExecutor m_CmdExecutor;
        private readonly CircularBuffer<HistoryData>? history;
        private readonly int id = Interlocked.Increment(ref idSeed);
        private readonly AtomicFlag awaitingFlag = new AtomicFlag(false);
        private long jobCount;
        private int historyIndex;

        public static JobDispatcher? AsyncLocalDispatcher => asyncLocalDispatcher.Value;
        public static void ClearAsyncLocal()
        {
            asyncLocalDispatcher.Value = null;
        }

        public JobDispatcher(bool useHistory)
        {
            if (useHistory)
            {
                history = new CircularBuffer<HistoryData>(4);
            }

            m_CmdExecutor = CommandExecutor.CreateCommandExecutor(nameof(JobDispatcher), id);
        }

        public JobDispatcher(bool useHistory, ICommandExecutor cmdExecutor)
        {
            if (useHistory)
            {
                history = new CircularBuffer<HistoryData>(4);
            }

            m_CmdExecutor = cmdExecutor;
        }

        public long JobCount => jobCount;
        public int Id => id;
        internal IEnumerable<HistoryData> History => history?.ToArray(historyIndex) ?? Enumerable.Empty<HistoryData>();

        public override string ToString()
        {
            return $"id:{id} count:{jobCount} historyIndex:{historyIndex}";
        }

        public void Pend(Action action)
        {
            m_CmdExecutor.Invoke(() =>
            {
                action.Invoke();
            });
        }

        public void Post(Action action, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Post(new JobMessage(
                action: action, 
                tag: TagBuilder.MakeTag(file, line)));
        }

        public void PostSync(Func<CustomTask> action, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Post(new JobMessageAsync(
                dispatcher: this,
                action: action,
                tag: TagBuilder.MakeTag(file, line)));
        }

        public Future PostFuture(Action action, IActor futureOwner, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var future = new Future();

            Post(new JobFutureMessage(
                action: action,
                future: future,
                futureOwner: futureOwner,
                tag: TagBuilder.MakeTag(file, line)));

            return future;
        }

        public Future<TResult> PostFuture<TResult>(Func<TResult> action, IActor futureOwner, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var future = new Future<TResult>();

            Post(new JobFutureMessageWithResult<TResult>(
                action: action, 
                future: future, 
                futureOwner: futureOwner,
                tag: TagBuilder.MakeTag(file, line)));

            return future;

        }

        public Future<TResult> PostFuture<TResult>(Func<CustomTask<TResult>> action, IActor futureOwner, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var future = new Future<TResult>();

            Post(new JobFutureAsyncMessageWithResult<TResult>(
                dispatcher: this,
                action: action,
                future: future,
                futureOwner: futureOwner,
                tag: TagBuilder.MakeTag(file, line)));

            return future;
        }

        public CustomTask<TResult> PostTask<TResult>(Func<TResult> func, IActor taskOwner, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var task = new CustomTask<TResult>(TagBuilder.MakeTag(file, line));

            Post(new JobTaskMessageWithResult<TResult>(
                action: func,
                task: task,
                taskOwner: taskOwner,
                tag: TagBuilder.MakeTag(file, line)));

            return task;
        }

        public bool Test()
        {
            return asyncLocalDispatcher.Value == this;
        }

        public void Post(IJobMessage message)
        {
            jobQueue.Enqueue(message);
            Interlocked.Increment(ref jobCount);

            if (awaitingFlag.IsOn &&
                message.IsAwait)
            {
                if (asyncLocalDispatcher.Value != null)
                {
                    if (asyncLocalDispatcher.Value == this)
                    {
                        throw new Exception($"duplicated self post, this.id:{id} asyncLocalid:{asyncLocalDispatcher.Value.id}");
                    } // 중복된 재귀적인 Post
                }
            } // 비동기 메시지의 재귀적인 post는 불가능.
        }

        public void PostDirect(Action action, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            PostDirect(new JobMessage(
                action: action,
                tag: TagBuilder.MakeTag(file, line)));
        }

        public void PostDirect(IJobMessage message)
        {
            jobQueue.Enqueue(message);
            long incremented = Interlocked.Increment(ref jobCount);
            if (incremented != 1)
            { // queue의 맨 앞에 job을 넣은 것이 아니라면 바로 리턴
                return;
            }

            if (asyncLocalDispatcher.Value != null)
            {
                if (asyncLocalDispatcher.Value == this)
                {
                    throw new Exception($"duplicated self post, this.id:{id} asyncLocalid:{asyncLocalDispatcher.Value.id}");
                } // 중복된 재귀적인 Post

                return;
                // post 하였는데 다른 액터의 post일 경우.
            }

            InvokeMessages();
        }

        public void ContinueAfterAwait()
        {
            Interlocked.Decrement(ref jobCount);
            awaitingFlag.Off();
        }

        private void AddHistory(string tag)
        {
            if (history == null)
            {
                return;
            }

            history[historyIndex++] = new HistoryData(tag);
        }

        private void InvokeMessages()
        {
            asyncLocalDispatcher.Value = this;

            while (jobQueue.IsEmpty == false)
            {
                if (jobQueue.TryDequeue(out IJobMessage? message) == false)
                {
                    throw new Exception($"logical error, id: {id}");
                }

                AddHistory(message.Tag);

                if (message.IsAwait)
                {
                    awaitingFlag.On();
                }

                message.Execute();

                if (message.IsAwait)
                {
                    asyncLocalDispatcher.Value = null;
                    return;
                } // Task형 리턴일 경우에 로직에 대한 순차 처리 및 소유권이 다른 스레드로 넘어가기 때문에 return 하여 다음 틱에 실행.
            }

            asyncLocalDispatcher.Value = null;
            ConsumeJobCount();
        }

        private void ConsumeJobCount()
        {
            long copyJobCount = jobCount;
            Interlocked.Add(ref jobCount, -copyJobCount);
        }

        public void RunAction()
        {
            Execute();
            m_CmdExecutor.Execute();
        }

        private void Execute()
        {
            var readCount = Interlocked.Read(ref jobCount);
            if (readCount < 0)
            {
                throw new Exception($"logical error, invalid job count: {readCount}, id: {id}");
            }

            if (readCount == 0)
            {
                return;
            }

            if (awaitingFlag.IsOn)
            {
                return;
            } // 비동기 로직이 아직 안끝났으므로 리턴.

            InvokeMessages();
        }

        public void Dispose()
        {

        }

        [DebuggerDisplay("[{Time.ToString(\"yyyy-MM-dd HH:mm:ss.fff\")}] {Tag}")]
        internal readonly struct HistoryData
        {
            public HistoryData(string tag)
            {
                Time = DateTime.Now;
                Tag = tag;
            }

            public DateTime Time { get; }
            public string Tag { get; }
        }
    }
}
