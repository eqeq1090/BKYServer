using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Logger;
using BKServerBase.Util;
using BKNetwork.ConstEnum;
using BKNetwork.Dispatch.Dispatcher;
using BKNetwork.Interface;
using BKNetwork.Serialize;
using BKProtocol;

namespace BKNetwork.Dispatch.Manager
{
    public class ClientDispatchManager
    {
        private OnPreDispatchEventHandler? m_DefaultPrevDispatcher;

        public Dictionary<MsgType, IDispatcher> MessageDispatcherMap = new Dictionary<MsgType, IDispatcher>();
        public ConcurrentDictionary<int, Dictionary<MsgType, IDispatcher>> SessionOnceDispatcherMap = new();
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

                //일반 디스패칭
                if (MessageDispatcherMap.TryGetValue(messageType, out var dispatcher) == false)
                {
                    stream.Seek(packetBodySize, SeekOrigin.Current);
                    CoreLog.Critical.LogError($"{session.Context} Invalid Message - {messageType} / {requiredMinPacketLength}");
                    continue;
                }

                using var subStream = new MemoryStream(stream.GetBuffer(), (int)stream.Position, packetBodySize);
                using var subStreamOnce = new MemoryStream(stream.GetBuffer(), (int)stream.Position, packetBodySize);
                stream.Seek(packetBodySize, SeekOrigin.Current);
                dispatcher.Fetch(session.Context!, subStream);

                //Once 디스패칭
                if (SessionOnceDispatcherMap.TryGetValue(session.ID, out var dictionary) == false)
                {
                    // CoreLog.Critical.LogWarning($"SessionOnceDispatcherMap not found. SessionID : {session.SessionID}");
                    continue;
                }
                if (dictionary.Remove(messageType, out var dispatcherOnce) == true)
                {
                    dispatcherOnce.Fetch(session.Context!, subStreamOnce);
                }
            }
            return true;
        }

        public bool LoopbackLink(ISession session, IMsg msg)
        {
            return true;
        }

        private void RegisterDispatcher<T>(MsgType msgType, OnClientDispatchEventHandler<T> handler) where T : IMsg, new()
        {
            if (true == MessageDispatcherMap.ContainsKey(msgType))
            {
                // Dup dispatcher
                CoreLog.Critical.LogWarning(new Exception($"[DispatcherManager] Duplicated MessageID Insertion : {msgType}"));
                return;
            }
            MessageDispatcherMap[msgType] = new ClientDispatcher<T>(handler, m_DefaultPrevDispatcher);
        }

        public void RegisterDispatcher<T>(OnClientDispatchEventHandler<T> handler, OnPreDispatchEventHandler? preDispatchEventHandler = null) where T : IMsg, new()
        {
            var msgType = new T().msgType;
            RegisterDispatcher(msgType, handler);
        }

        public void SetDefaultPreDispatcher(OnPreDispatchEventHandler handler)
        {
            m_DefaultPrevDispatcher = handler;
        }

        public void SetPreSendEventHandler(OnPreSendEventHandler handler)
        {

        }

        public void RegisterDispatcherOnce<T>(int sessionID, OnClientDispatchEventHandler<T> handler) 
            where T : IMsg, new()
        {
            if (SessionOnceDispatcherMap.TryGetValue(sessionID, out var dictionary) == false)
            {
                dictionary = new Dictionary<MsgType, IDispatcher>();
                SessionOnceDispatcherMap.TryAdd(sessionID, dictionary);
            }

            var msgType = new T().msgType;
            dictionary.TryAdd(msgType, new ClientDispatcher<T>(handler, m_DefaultPrevDispatcher));
        }

        public void UnregisterDispatcherOnce<T>(int sessionID)
            where T : IMsg, new()
        {
            if (SessionOnceDispatcherMap.TryGetValue(sessionID, out var dictionary) == false)
            {
                CoreLog.Critical.LogError($"UnregisterDispatcherOnce failed, data is empty");
                return;
            }

            var msgType = new T().msgType;
            dictionary.Remove(msgType, out _);
        }

        public void RemoveOnceDispatcherGroup(int sessionID)
        {
            if (SessionOnceDispatcherMap.ContainsKey(sessionID) == false)
            {
                CoreLog.Critical.LogError($"UnregisterDispatcherOnce failed, data is empty");
                return;
            }
            SessionOnceDispatcherMap.Remove(sessionID, out var dictionary);
            if (dictionary != null)
            {
                foreach (var item in dictionary.Keys)
                {
                    dictionary.Remove(item, out _);
                }
            }
        }

        public void RegisterDispatcherOnce<T>(long targetPacketUID, OnTargetDispatchEventHandler<T> handler) 
            where T : ITargetResMsg, new()
        {
            var msgType = new T().msgType;

            if (MessageDispatcherMap.TryGetValue(msgType, out var dispatcher) == false)
            {
                dispatcher = new ClientDispatcher<T>();
                MessageDispatcherMap.TryAdd(msgType, dispatcher);
            }
            
            if (dispatcher is not ClientDispatcher<T> clientDispatcher)
            {
                CoreLog.Critical.LogError($"RegisterDispatcherOnce failed, dispatcher is not ClientDispatcher");
                return;
            }

            clientDispatcher.RegisterOnceHandler(targetPacketUID, handler);
        }

        public void UnregisterDispatcherOnce<T>(long targetPacketUID)
            where T : ITargetResMsg, new()
        {
            var msgType = new T().msgType;

            if (MessageDispatcherMap.TryGetValue(msgType, out var dispatcher) == false)
            {
                CoreLog.Critical.LogError($"UnregisterDispatcherOnce failed, msgType is not registered, msgType: {msgType}");
                return;
            }

            if (dispatcher is not ClientDispatcher<T> clientDispatcher)
            {
                CoreLog.Critical.LogError($"dispatcher is not ClientDispatcher");
                return;
            }

            clientDispatcher.UnregisterOnceHandler(targetPacketUID);
        }
    }
}
