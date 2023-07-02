using BKServerBase.Logger;
using System.Net;
using BKProtocol;
using BKServerBase.Util;
using BKNetwork.Interface;
using BKNetwork.ConstEnum;
using BKNetwork.Sock;
using BKNetwork.Serialize;
using BKProtocol.Enum;
using System.Collections.Concurrent;
using BKNetwork.Dispatch.Dispatcher;
using BKGameServerComponent.Actor;

namespace BKGameServerComponent.MsgRegister
{
    internal class PubSubDispatchManager
    {
        private OnPubsubPreDispatchEventHandler? OnDefaultPreDispatcherHandler = null;

        public Dictionary<PubsubMsgType, IPubsubDispatcher> MessageDispatcherMap = new Dictionary<PubsubMsgType, IPubsubDispatcher>();

        public void SetDefaultPreDispatcher(OnPubsubPreDispatchEventHandler handler)
        {
            OnDefaultPreDispatcherHandler += handler;
        }

        public void RegisterDispatcher<T>(OnPubsubDispatchEventHandler<T> handler, OnPubsubPreDispatchEventHandler? preDispatchEventHandler = null)
            where T : IPubsubMsg, new()
        {
            var msg = new T();

            if (MessageDispatcherMap.ContainsKey(msg.MsgType))
            {
                // Dup dispatcher
                CoreLog.Critical.LogWarning(new Exception($"[PubSubDispatchManager] Duplicated MessageID Insertion : {msg.MsgType}"));
                return;
            }

            preDispatchEventHandler += OnDefaultPreDispatcherHandler;
            MessageDispatcherMap[msg.MsgType] = new PubSubMsgDispatcher<T>(handler, preDispatchEventHandler);
            CoreLog.Normal.LogDebug($"[PubSubDispatchManager] Registered Message : {msg.MsgType} - Total Count : {MessageDispatcherMap.Count}");
        }

        public void Dispatch(Player player, IPubsubMsg msg)
        {
            if (MessageDispatcherMap.TryGetValue(msg.MsgType, out var dispatcher) == false)
            {
                return;
            }
            dispatcher.Fetch(player, msg);
        }
    }
}
