using System;

namespace BKNetwork.Pool
{
    public abstract class IAsyncIOContext : IDisposable
    {
        protected bool m_Disposed;
        public IAsyncIOContext()
        {
            m_Disposed = false;
        }

        public void Dispose()
        {
            if (true == m_Disposed)
            {
                return;
            }
            Dispose(true);
            m_Disposed = true;
            GC.SuppressFinalize(this);
        }

        public abstract void Reset();
        protected abstract void Dispose(bool isDisposing);
    }
}
