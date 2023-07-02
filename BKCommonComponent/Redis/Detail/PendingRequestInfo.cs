using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKCommonComponent.Redis.Detail
{
    public sealed class PendingRequestInfo<T>
        where T : class
    {
        private readonly TaskCompletionSource<T> m_Tcs;

        public PendingRequestInfo(TaskCompletionSource<T> tcs)
        {
            m_Tcs = tcs;
        }

        public void Resolove(T connector)
        {
            m_Tcs.SetResult(connector);
        }
    }
}
