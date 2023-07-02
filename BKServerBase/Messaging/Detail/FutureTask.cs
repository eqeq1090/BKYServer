using System;
using System.Threading.Tasks;

namespace BKServerBase.Messaging.Detail
{
    public class FutureTask<T> 
    {
        private TaskCompletionSource<T> awaitTask = new TaskCompletionSource<T>();
        public Task<T> Result => awaitTask.Task;

        public void Resolve(T result)
        {
            awaitTask.SetResult(result);
        }

        public void ResolveFail(Exception e)
        {
            awaitTask.SetException(e);
        }
    }
}
