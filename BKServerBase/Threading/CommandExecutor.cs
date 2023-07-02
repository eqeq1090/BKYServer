using BKServerBase.Logger;
using BKServerBase.Util;
using System;
using System.Collections.Concurrent;
using Prometheus;

namespace BKServerBase.Threading
{
    public class CommandExecutor : ICommandExecutor, IDisposable
    {
        private static CommandExecutor? dummy;
        private static object syncRoot = new object();
        public static CommandExecutor DummyCommandExecutor
        {
            get
            {
                if (dummy == null)
                {
                    lock (syncRoot)
                    {
                        if (dummy == null)
                        {
                            dummy = new CommandExecutor();
                            dummy.Close();
                        }
                    }
                }
                return dummy;
            }
        }

        public static CommandExecutor CreateCommandExecutor(string name, long objectID)
        {
            return new CommandExecutor(name, objectID);
        }

        private readonly string m_name;
        private readonly long m_ownerObjectID;
        private volatile bool m_closed = false;
        private ConcurrentQueue<QueueCommand> PendingCommnadQueue = new ConcurrentQueue<QueueCommand>();
        private bool disposedValue;

        private static readonly Counter s_counter = Metrics
        .CreateCounter("executor_remain_count", "Command Executor", "kind");

        protected CommandExecutor()
        {
            m_name = "Dummy";
            m_ownerObjectID = 0;
        }

        protected CommandExecutor(string name, long ObjectID)
        {
            ObjectCounter<CommandExecutor>.Increment();
            m_name = name;
            m_ownerObjectID = ObjectID;
            CoreLog.Normal.LogDebug($"{ToString()} Created.");
        }

        ~CommandExecutor()
        {
            Dispose(false);
        }

        public override string ToString()
        {
            return $"CommandExecutor: {m_name} :ObjectID({m_ownerObjectID})";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                    s_counter.WithLabels(m_name).Inc(PendingCommnadQueue.Count);

                    while (PendingCommnadQueue.TryDequeue(out _)) ;
                }
                disposedValue = true;
                ObjectCounter<CommandExecutor>.Decrement();
                CoreLog.Normal.LogDebug($"{ToString()} Disposed.");
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool IsEmpty()
        {
            return PendingCommnadQueue.IsEmpty;
        }

        public long Length()
        {
            return PendingCommnadQueue.Count;
        }

        public void Close()
        {
            m_closed = true;
        }

        public bool Invoke(Command command)
        {
            if (m_closed) return false;
            PendingCommnadQueue.Enqueue(new QueueCommand(command));
            return true;
        }

        public void Execute()
        {
            PumpQueuedCommand();
        }

        private void PumpQueuedCommand()
        {
            while (PendingCommnadQueue.TryDequeue(out var command))
            {
                command.Execute();
            }
        }
    }
}
