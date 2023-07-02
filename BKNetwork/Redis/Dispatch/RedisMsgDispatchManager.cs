using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Logger;
using BKServerBase.Util;
using BKNetwork.Dispatch;
using BKNetwork.Serialize;
using BKProtocol;
using Newtonsoft.Json.Linq;
using BKProtocol.Enum;

namespace BKNetwork.Redis.Dispatch
{
    public class RedisMsgDispatchManager
    {
        public Dictionary<PubsubMsgType, IRedisMsgDispatcher> MessageDispatcherMap = new Dictionary<PubsubMsgType, IRedisMsgDispatcher>();

        public void RegisterDispatcher<T>(OnRedisMsgDispatchHandler<T> handler)
        where T : IPubsubMsg, new()
        {
            var msgType = new T().MsgType;
            RegisterDispatcher(msgType, handler);
        }

        private void RegisterDispatcher<T>(PubsubMsgType msgType, OnRedisMsgDispatchHandler<T> handler)
            where T : IPubsubMsg, new()
        {
            if (true == MessageDispatcherMap.ContainsKey(msgType))
            {
                // Dup dispatcher
                CoreLog.Critical.LogWarning(new Exception($"[DispatcherManager] Duplicated MessageID Insertion : {msgType}"));
                return;
            }
            MessageDispatcherMap[msgType] = new RedisMsgDispatcher<T>(handler);
            CoreLog.Normal.LogDebug($"[DispatcherManager] Registered Message : {new T().MsgType}/{msgType} - Total Count : {MessageDispatcherMap.Count}");
        }

        public void Dispatch(IPubsubMsg message)
        {
            if (MessageDispatcherMap.TryGetValue(message.MsgType, out var dispatcher) == false)
            {
                //ERROR
                return;
            }
            dispatcher.Fetch(message);
        }
    }
}
