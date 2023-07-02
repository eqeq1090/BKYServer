using BKServerBase.Logger;
using BKProtocol;
using BKNetwork.Interface;
using BKNetwork.Serialize;
using BKProtocol.Enum;
using Newtonsoft.Json;
using BKGameServerComponent.Actor;

namespace BKGameServerComponent.MsgRegister
{
    internal class PubSubMsgDispatcher<T> : IPubsubDispatcher
        where T : IPubsubMsg, new()
    {
        private OnPubsubDispatchEventHandler<T>? DispatchHandler;
        private OnPubsubPreDispatchEventHandler? PreDispatchEventHandler;

        public PubSubMsgDispatcher(OnPubsubDispatchEventHandler<T> handler, OnPubsubPreDispatchEventHandler? preDispatchEventHandler)
        {
            DispatchHandler = handler;
            PreDispatchEventHandler = preDispatchEventHandler;
        }

        public bool Fetch(Player player, IPubsubMsg msg)
        {
            if (msg is not T targetMsg)
            {
                return false;
            }
            try
            {
                DispatchHandler?.Invoke(player, targetMsg);
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