using EmbedIO.Sessions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKNetwork.ConstEnum;
using BKNetwork.Dispatch.Manager;
using BKNetwork.Interface;
using BKNetwork.Session;
using BKProtocol;
using BKProtocol.Enum;

namespace BKCommonComponent.Node
{
    public sealed class ServerNode : IContext
    {
        private static int SessionIDCounter;

        private readonly ServerMode m_ServerMode;
        private readonly AtomicFlag m_ConnectingFlag = new AtomicFlag(false);
        private readonly ClientDispatchManager m_ClientDispatchManager;
        private TCPClientSession m_ClientSession;

        public ServerNode(
            ServerMode serverMode,
            IPEndPoint remoteEndPoint,
            ClientDispatchManager clientDispatchManager)
        {
            m_ServerMode = serverMode;
            RemoteEndPoint = remoteEndPoint;
            m_ClientDispatchManager = clientDispatchManager;

            var sessionID = MakeSessionID();
            m_ClientSession = new TCPClientSession(sessionID, clientDispatchManager, this, OnConnect, OnClose);

            RemoteHost = remoteEndPoint.ToString();
        }

        public long LastKeepAliveTick { get; set; }
        public IPEndPoint RemoteEndPoint { get; }
        public BKNetwork.Interface.ISession Session => m_ClientSession;
        public ServerMode ServerMode => m_ServerMode;
        public bool IsConnected => m_ClientSession.IsRunning();
        public bool IsConnecting => m_ConnectingFlag.IsOn;
        public string RemoteHost { get; }
        public int ServerID { get; private set; }

        private static int MakeSessionID()
        {
            return Interlocked.Increment(ref SessionIDCounter);
        }

        public void BeginClose(DisconnectReason reason, string reasonDesc = "")
        {
            GameNetworkLog.Critical.LogInfo($"BeginClose session({m_ClientSession.ID}), reason: {reason}, desc: {reasonDesc}");

            m_ClientSession.BeginClose(reason);
        }

        public void Close(DisconnectReason reason, string reasonDesc = "")
        {
            GameNetworkLog.Critical.LogInfo($"Close session({m_ClientSession.ID}), reason: {reason}, desc: {reasonDesc}");

            m_ClientSession.Close(reason);
        }

        public SendResult Send(IMsg msg)
        {
            return m_ClientSession.SendLink(msg);
        }

        public int GetSessionID()
        {
            return m_ClientSession.ID;
        }

        public long GetUserUID()
        {
            return KeyGenerator.Issue();
        }

        public bool Connect(int retry)
        {
            if (IsConnected)
            {
                return true;
            }

            CoreLog.Normal.LogInfo($"Try to connect for {m_ServerMode}@{RemoteEndPoint}");

            int retryCount = 0;

            while (retryCount < retry || retry == Timeout.Infinite)
            {
                bool result = m_ClientSession.Connect(RemoteEndPoint);
                if (result is false)
                {
                    CoreLog.Critical.LogInfo($"Retrying to connect for {m_ServerMode}@{RemoteEndPoint} ... retryCount: {retryCount}");
                    Task.Delay(300).Wait();
                    ++retryCount;
                    continue;
                }

                break;
            }

            if (retryCount == retry)
            {
                CoreLog.Critical.LogInfo($"Connection is failed for {m_ServerMode}@{RemoteEndPoint}");
                return false;
            }

            return true;
        }

        public bool Reconnect()
        {
            if (IsConnected)
            {
                return true;
            }

            if (m_ConnectingFlag.On() is false)
            {
                return false;
            } // 재접속 시도 중.

            m_ClientSession.Close(DisconnectReason.ByClient);
            m_ClientSession.Dispose();

            var sessionID = MakeSessionID();
            m_ClientSession = new TCPClientSession(sessionID, m_ClientDispatchManager, this, OnConnect, OnClose);

            var result = Connect(Timeout.Infinite);

            m_ConnectingFlag.Off();

            return result;
        }

        private void OnConnect()
        {
            CoreLog.Normal.LogInfo($"Connection is established for {m_ServerMode}@{RemoteEndPoint}");

            m_ClientSession.SetContext(this);

            LastKeepAliveTick = DateTime.Now.Ticks;
        }

        private void OnClose()
        {
            CoreLog.Critical.LogInfo($"ServerNode is closed for {m_ServerMode}@{RemoteEndPoint}");

            BackgroundJob.Execute(() =>
            {
                Reconnect();
                // 서버간 연결이 끊어진 경우, 성공할 떄 까지 계속 시도.
            });
        }

        public void Dispose()
        {
            m_ClientSession.ClientSocket.Dispose();
        }

        public void CloseAsync(DisconnectReason reason)
        {
            Close(reason);
        }

        public SendResult SendError<TResponse>(MsgErrorCode errorCode) where TResponse : IResMsg, new()
        {
            return m_ClientSession.SendError<TResponse>(errorCode);
        }

        public SendResult SendError<TResponse>(MsgErrorCode errorCode, long targetPacketUID) where TResponse : ITargetResMsg, new()
        {
            return m_ClientSession.SendError<TResponse>(errorCode, targetPacketUID);
        }

        public void Update()
        {

        }

        public void SetServerID(int serverID)
        {
            ServerID = serverID;
        }
    }
}
