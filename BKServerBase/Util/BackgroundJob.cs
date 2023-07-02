using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Util
{
    public static class BackgroundJob
    {
        public static void Execute(Action action)
        {
            using (var control = ExecutionContext.SuppressFlow())
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                });
            }
        }

        public static void Execute(Func<Task> function)
        {
            using (var control = ExecutionContext.SuppressFlow())
            {
                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    try
                    {
                        await function();
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                });
            }
        }
    }
}
