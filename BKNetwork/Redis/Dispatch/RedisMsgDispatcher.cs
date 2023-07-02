using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Logger;
using BKNetwork.Dispatch;
using BKNetwork.Interface;
using BKNetwork.Serialize;
using BKProtocol;

namespace BKNetwork.Redis.Dispatch
{
    public delegate void OnRedisMsgDispatchHandler<T>(T msg)
        where T : IPubsubMsg, new();

    public interface IRedisMsgDispatcher
    {
        bool Fetch(IPubsubMsg msg);
    }

    public class RedisMsgDispatcher<T> : IRedisMsgDispatcher
        where T : IPubsubMsg, new()
    {
        private OnRedisMsgDispatchHandler<T> DispatchHandler;

        public RedisMsgDispatcher(OnRedisMsgDispatchHandler<T> handler)
        {
            DispatchHandler = handler;
        }

        public bool Fetch(IPubsubMsg msg)
        {
            if (!(msg is T))
            {
                return false;
            }
            try
            {
                DispatchHandler((msg as T)!);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogFatal(e);
                return false;
            }
        }
    }
}
