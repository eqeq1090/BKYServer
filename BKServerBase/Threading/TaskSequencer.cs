using BKServerBase.Logger;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;

namespace BKServerBase.Threading
{
    public class TaskSequencer
    {
        private AtomicFlag m_Close = new AtomicFlag(false);
        private AtomicFlag m_Running = new AtomicFlag(false);
        private Queue<Command> m_PendingCommandQueue = new Queue<Command>();
        public delegate void FinishCallback();
        public delegate void Command(FinishCallback next);
        public TaskSequencer()
        {

        }

        public void SetThread(Thread thread)
        { }

        public bool Enqueue(Command command)
        {
            if (m_Close.IsOn)
            {
                return false;
            }
            m_PendingCommandQueue.Enqueue(command);
            Execute();
            return true;
        }

        public bool IsEmpty()
        {
            return m_PendingCommandQueue.Count == 0;
        }

        public long Length()
        {
            return m_PendingCommandQueue.Count;
        }

        public void Destroy()
        {
            m_Close.On();
            m_Running.Off();
            m_PendingCommandQueue.Clear();
        }

        private void Execute()
        {
            if (m_Running.IsOn)
            {
                return;
            }

            if (m_PendingCommandQueue.TryPeek(out var command) is false)
            {
                return;
            }

            if (m_Running.On() is false)
            {
                return;
            }

            command(() =>
            {
                if (m_Close.IsOn)
                {
                    return;
                }

                if (m_PendingCommandQueue.TryDequeue(out var executedCommand) is false)
                {
                    throw new TaskSequencerException("QueueCountIsZero");
                }
                if (executedCommand != command)
                {
                    throw new TaskSequencerException("QueueHeadChanged");
                }
                m_Running.Off();
                Execute();
            });
        }
    }

    [Serializable]
    public class TaskSequencerException : Exception
    {
        public TaskSequencerException()
        { }

        public TaskSequencerException(string? message) : base(message)
        { }

        public TaskSequencerException(string? message, Exception? innerException) : base(message, innerException)
        { }

        protected TaskSequencerException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}
