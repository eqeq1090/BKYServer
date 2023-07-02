using BKServerBase.Logger;
using BKProtocol;
using BKNetwork.Interface;
using BKNetwork.Serialize;

namespace BKNetwork.Dispatch.Dispatcher
{
    public class ClientDispatcher<T> : IDispatcher
        where T : IMsg, new()
    {
        private OnClientDispatchEventHandler<T>? DispatchHandler;
        private OnPreDispatchEventHandler? m_PrevDispatcher;
        private Dictionary<long, OnTargetDispatchEventHandler<T>> OnceDispatchHandlers = new Dictionary<long, OnTargetDispatchEventHandler<T>>();

        public ClientDispatcher(OnClientDispatchEventHandler<T> handler, OnPreDispatchEventHandler? prevDispatcher)
        {
            DispatchHandler = handler;
            m_PrevDispatcher = prevDispatcher;
        }

        public ClientDispatcher()
        {
        }

        public bool Fetch(IContext context, MemoryStream memoryStream)
        {
            try
            {
                var msg = Serializer.Deserialize<T>(memoryStream);

                m_PrevDispatcher?.Invoke(msg, context);

                if (msg is ITargetResMsg targetResMsg)
                {
                    if (OnceDispatchHandlers.Remove(targetResMsg.targetPacketUID, out var targetHandler))
                    {
                        targetHandler.Invoke(msg, context);
                    }
                }

                DispatchHandler?.Invoke(context, msg);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogFatal(e);
                return false;
            }
        }

        public bool Fetch(IContext context, IMsg msg)
        {
            if (!(msg is T))
            {
                return false;
            }
            try
            {
                m_PrevDispatcher?.Invoke(msg, context);
                DispatchHandler?.Invoke(context, (msg as T)!);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogFatal(e);
                return false;
            }
        }

        public bool RegisterOnceHandler(long targetPacketUID, OnTargetDispatchEventHandler<T> handler)
        {
            return OnceDispatchHandlers.TryAdd(targetPacketUID, handler);
        }

        public bool UnregisterOnceHandler(long targetPacketUID)
        {
            return OnceDispatchHandlers.Remove(targetPacketUID);
        }
    }
}