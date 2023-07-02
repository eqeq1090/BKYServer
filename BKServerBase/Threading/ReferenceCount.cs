using BKServerBase.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BKServerBase.Threading
{
    public enum RefCountReason
    {
        Instantiate,
        SocketConnect,
        SocketDisconnect,
        SocketSend,
        SocketReceive,
        Max,
    }
    public class ReferenceCount
    {
        private int refCount;
        private int[] refCountsPerReason = new int[(int)RefCountReason.Max];
        private Action zeroRefCallback;
        public ReferenceCount(Action zeroRefCallback)
        {
            this.zeroRefCallback = zeroRefCallback;
        }
        public void Increment(RefCountReason reason)
        {
            Interlocked.Increment(ref refCount);
            Interlocked.Increment(ref refCountsPerReason[(int)reason]);
#if DEBUG
            WriteLog();
#endif
        }
        public void Decrement(RefCountReason reason)
        {
            var result = Interlocked.Decrement(ref refCount);
            Interlocked.Decrement(ref refCountsPerReason[(int)reason]);
#if DEBUG
            WriteLog();
#endif
            if (result == 0)
            {
                zeroRefCallback.Invoke();
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"refCount : {Volatile. Read(ref refCount)}");
            for (int i = 0; i < (int)RefCountReason.Max; ++i)
            {
                sb.Append($", ");
                sb.Append($"{(RefCountReason)i} : {Volatile.Read(ref refCountsPerReason[i])}");
            }
            return sb.ToString();
        }

        private void WriteLog()
        {
            //CoreLog.Normal.LogDebug($"RefCount Log : {ToString()}");
        }
    }
}
