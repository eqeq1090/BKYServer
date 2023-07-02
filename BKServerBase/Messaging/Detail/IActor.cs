using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Messaging.Detail
{
    public interface IActor
    {
        JobDispatcher GetDispatcher();

        void Post(Action action) 
        {
            var jobDispatcher = GetDispatcher();
            jobDispatcher.Post(() => action.Invoke());
        }
    }
}
