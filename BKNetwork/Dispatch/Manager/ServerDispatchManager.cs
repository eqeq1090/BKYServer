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

namespace BKNetwork.Dispatch.Manager
{
    public delegate bool OnSocketBufferOverflowEventHandler(ISocket socket, ServerDispatchManager manager);
    public delegate bool OnInvalidMessageEventHandler(ISession session, ServerDispatchManager manager, MsgType MsgType, ref long recvLength, uint reqLength);

    public class ServerDispatchManager
    {
        private OnPreDispatchEventHandler? OnDefaultPreDispatcherHandler = null;

        public Dictionary<MsgType, IDispatcher> MessageDispatcherMap = new Dictionary<MsgType, IDispatcher>();
        public OnSocketBufferOverflowEventHandler? OnBufferOverflowHandler = null;
        public OnInvalidMessageEventHandler? OnInvalidMessageHandler = null;
        public OnPreSendEventHandler? OnPreSendEventHandler = null;

        public void SetDefaultPreDispatcher(OnPreDispatchEventHandler handler)
        {
            OnDefaultPreDispatcherHandler += handler;
        }
        public void SetPreSendEventHandler(OnPreSendEventHandler handler)
        {
            OnPreSendEventHandler += handler;
        }
        public void SetSocketBufferOverflowHandler(OnSocketBufferOverflowEventHandler handler)
        {
            OnBufferOverflowHandler += handler;
        }
        public void SetSocketInvalidMessageHandler(OnInvalidMessageEventHandler handler)
        {
            OnInvalidMessageHandler += handler;
        }

        public void RegisterDispatcher<T>(OnDispatchEventHandler<T> handler, OnPreDispatchEventHandler? preDispatchEventHandler = null, ParsingType parsingType = ParsingType.MsgPack)
            where T : IMsg, new()
        {
            var msg = new T();
            msg.ParsingType = parsingType;

            if (MessageDispatcherMap.ContainsKey(msg.msgType))
            {
                // Dup dispatcher
                CoreLog.Critical.LogWarning(new Exception($"[DispatcherManager] Duplicated MessageID Insertion : {msg.msgType}"));
                return;
            }

            preDispatchEventHandler += OnDefaultPreDispatcherHandler;
            MessageDispatcherMap[msg.msgType] = new ServerDispatcher<T>(handler, preDispatchEventHandler, msg.ParsingType);
            CoreLog.Normal.LogDebug($"[DispatcherManager] Registered Message : {msg.msgType} - Total Count : {MessageDispatcherMap.Count}");
        }

        public void Dispatch(ISession session, IMsg msg)
        {
            if (MessageDispatcherMap.TryGetValue(msg.msgType, out var dispatcher) == false)
            {
                return;
            }
            if (session.Context == null)
            {
                //ERROR (루프백에 대한 처리인데 컨텍스트가 이미 망가진 상태이다.)
                return;
            }
            dispatcher.Fetch(session.Context, msg);
        }

        public bool Dispatch(ISession session, MemoryStream stream)
        {
            long totalReceivedStreamLength = stream.Length;
            using var reader = new BinaryReader(stream);
            while (0 < totalReceivedStreamLength)
            {
                if (Consts.PACKET_MIN_SIZE > totalReceivedStreamLength)
                {
                    CoreLog.Critical.LogWarning($"{session.Context} Invalid Message Size - PACKET_MIN_SIZE > {totalReceivedStreamLength}");
                    return false;
                }
                int requiredMinPacketLength = 0;
                Serializer.Load(reader, ref requiredMinPacketLength);

                if (totalReceivedStreamLength < requiredMinPacketLength)
                {
                    CoreLog.Critical.LogWarning($"{session.Context} Invalid Message Size - {totalReceivedStreamLength} < {requiredMinPacketLength}");
                    return false;
                }

                if (requiredMinPacketLength < Consts.PACKET_MIN_SIZE)
                {
                    CoreLog.Critical.LogWarning($"{session.Context} Invalid Message Size - {requiredMinPacketLength} < PACKET_MIN_SIZE({Consts.PACKET_MIN_SIZE})");
                    return false;
                }

                int MsgTypeInt = 0;
                Serializer.Load(reader, ref MsgTypeInt);

                var messageType = CastTo<MsgType>.From(MsgTypeInt);
                int packetBodySize = requiredMinPacketLength - Consts.PACKET_LENGTH_SIZE - Consts.PACKET_MSG_TYPE_SIZE;
                totalReceivedStreamLength -= requiredMinPacketLength;

                if (false == MessageDispatcherMap.TryGetValue(messageType, out var dispatcher))
                {
                    stream.Seek(packetBodySize, SeekOrigin.Current);
                    CoreLog.Critical.LogError($"{session.Context} Invalid Message - {messageType} / {requiredMinPacketLength}");
                    if ((OnInvalidMessageHandler?.Invoke(session, this, messageType, ref totalReceivedStreamLength, (uint)requiredMinPacketLength) ?? true) == false)
                    {
                        return false;
                    }
                    continue;
                }

                using var subStream = new MemoryStream(stream.GetBuffer(), (int)stream.Position, packetBodySize);
                stream.Seek(packetBodySize, SeekOrigin.Current);
                dispatcher.Fetch(session.Context!, subStream);
            }
            return true;
        }

        public bool LoopbackLink(ISession session, IMsg msg)
        {
            Dispatch(session, msg);
            return true;
        }

        public void RegisterDispatcherOnce<T>(long targetPacketUID, OnTargetDispatchEventHandler<T> handler, ParsingType parsingType = ParsingType.MsgPack)
            where T : IMsg, new()
        {
            var msg = new T();
            msg.ParsingType = parsingType;

            if (MessageDispatcherMap.TryGetValue(msg.msgType, out var serverDispatcher) == false)
            {
                // Dup dispatcher
                serverDispatcher = new ServerDispatcher<T>(msg.ParsingType);
                MessageDispatcherMap.TryAdd(msg.msgType, serverDispatcher);
            }
            if (serverDispatcher is not ServerDispatcher<T> convertedDispatcher)
            {
                CoreLog.Critical.LogError($"[DispatcherManager] Registered Message : {msg.msgType} is not typed with ServerDispatcher");
                return;
            }
            convertedDispatcher.RegisterOnceHandler(targetPacketUID, handler);
        }

        public void UnregisterDispatcherOnce<T>(long targetPacketUID)
           where T : IMsg, new()
        {
            var msg = new T();
            if (MessageDispatcherMap.TryGetValue(msg.msgType, out var serverDispatcher) == false)
            {
                CoreLog.Critical.LogError($"[DispatcherManager] Registered Message : {msg.msgType} not found for UnregisterDispatcherOnce");
                return;
            }
            if (serverDispatcher is not ServerDispatcher<T> convertedDispatcher)
            {
                CoreLog.Critical.LogError($"[DispatcherManager] Registered Message : {msg.msgType} is not typed with ServerDispatcher");
                return;
            }
            convertedDispatcher.UnregisterOnceHandler(targetPacketUID);
        }
    }
}
