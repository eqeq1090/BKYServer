using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BKServerBase.Synchorization
{
    public sealed class Signal
    {
        private static Action CompletedAction = delegate { };
        private volatile Action? action;

        public bool IsCompleted => ReferenceEquals(action, CompletedAction);

        public void OnCompleted(Action reserveAction)
        {
            var oldAction = Interlocked.CompareExchange(ref action, reserveAction, null);
            if (ReferenceEquals(oldAction, CompletedAction))
            {
                reserveAction.Invoke();
            }
        }

        public void Set()
        {
            var oldAction = Interlocked.Exchange(ref action, CompletedAction);
            if (oldAction != null && ReferenceEquals(oldAction, CompletedAction) == false)
            {
                oldAction.Invoke();
            }
        }
    }

    public sealed class Signal<T>
    {
        private static Action<T> CompletedAction = delegate { };
        private volatile Action<T>? action = null;
        private T result = default(T)!;

        public bool IsCompleted => ReferenceEquals(action, CompletedAction);

        public void OnCompleted(Action<T> reserveAction)
        {
            var oldAction = Interlocked.CompareExchange(ref action, reserveAction, null);
            if (ReferenceEquals(oldAction, CompletedAction))
            {
                reserveAction.Invoke(result!);
            }
        }

        public void Set(T result)
        {
            this.result = result;

            var oldAction = Interlocked.Exchange(ref action, CompletedAction);
            if (oldAction != null && ReferenceEquals(oldAction, CompletedAction) == false)
            {
                oldAction.Invoke(result);
            }
        }
    }
}
