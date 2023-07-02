using BKServerBase.Logger;
using BKProtocol;
using BKNetwork.Interface;
using BKNetwork.Serialize;
using BKProtocol.Enum;
using Newtonsoft.Json;

namespace BKNetwork.Dispatch.Dispatcher
{
    public class ServerDispatcher<T> : IDispatcher
        where T : IMsg, new()
    {
        private OnDispatchEventHandler<T>? DispatchHandler;
        private OnPreDispatchEventHandler? PreDispatchEventHandler;
        private ParsingType m_ParsingType;
        private Dictionary<long, OnTargetDispatchEventHandler<T>> OnceDispatchHandlers = new Dictionary<long, OnTargetDispatchEventHandler<T>>();

        public ServerDispatcher(OnDispatchEventHandler<T> handler, OnPreDispatchEventHandler? preDispatchEventHandler, ParsingType parsingType)
        {
            DispatchHandler = handler;
            PreDispatchEventHandler = preDispatchEventHandler;
            m_ParsingType = parsingType;
        }

        public ServerDispatcher(ParsingType parsingType)
        {
            m_ParsingType = parsingType;
        }

        public bool Fetch(IContext context, MemoryStream memoryStream)
        {
            try
            {
                T? msg;

                if (m_ParsingType == ParsingType.Json)
                {
                    using var reader = new StreamReader(memoryStream);
                    var json = reader.ReadToEnd();
                    msg = JsonConvert.DeserializeObject<T>(json);
                }
                else
                {
                    msg = Serializer.Deserialize<T>(memoryStream);
                }

                if (msg is null)
                {
                    GameNetworkLog.Critical.LogFatal($"packet parsing failed, parsingType: {m_ParsingType}");
                    return false;
                }

                PreDispatchEventHandler?.Invoke(msg, context);
                if (msg is ITargetResMsg targetResMsg)
                {
                    if (OnceDispatchHandlers.Remove(targetResMsg.targetPacketUID, out var handler) == true)
                    {
                        handler.Invoke(msg, context);
                    }
                }
                DispatchHandler?.Invoke(msg, context, false);
                return true;
            }
            catch (Exception e)
            {
                GameNetworkLog.Critical.LogFatal(e);
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

        public bool Fetch(IContext context, IMsg msg)
        {
            if (!(msg is T))
            {
                return false;
            }
            try
            {
                DispatchHandler?.Invoke((msg as T)!, context, true);
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