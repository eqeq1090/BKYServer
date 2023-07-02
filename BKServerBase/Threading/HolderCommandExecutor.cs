using BKServerBase.Interface;
using System.Collections.Concurrent;

namespace BKServerBase.Threading
{
    public delegate void HolderCommand<T>(T holder) where T : IExecutorHolder;
    public class HolderCommandExecutor<T>
    where T : IExecutorHolder
    {
        private ConcurrentQueue<HolderCommand<T>> m_CommnadQueue = new ConcurrentQueue<HolderCommand<T>>();
        public void Execute(T holder)
        {
            while (m_CommnadQueue.TryDequeue(out var command))
            {
                command(holder);
            }
        }
        public bool IsEmpty()
        {
            return m_CommnadQueue.IsEmpty;
        }
        public bool Invoke(HolderCommand<T> command)
        {
            m_CommnadQueue.Enqueue(command);
            return true;
        }
    }
}
