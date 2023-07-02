using System.Net;
using System.Net.Sockets;
using BKNetwork.ConstEnum;
using BKNetwork.Dispatch.Manager;
using BKNetwork.Interface;
using BKNetwork.Sock;
using BKNetwork.Sock.TCP;
using BKProtocol;

namespace BKNetwork.Session
{
    public class TCPClientSession : ISession, IDisposable
    {
        public int ID { get; private set; }

        public ClientDispatchManager DispatchManagerCtx { get; init; }

        public TCPClientSocket ClientSocket { get; private set; }

        public TCPClientConnectState ConnectState { get; private set; }

        public IContext Context { get; init; }

        private OnConnect m_Onconnect;
        private OnClose m_OnClose;

        public TCPClientSession(int sessionID, ClientDispatchManager dispatcherManager, IContext context, OnConnect onConnect, OnClose onClose)
        {
            DispatchManagerCtx = dispatcherManager;
            Context = context;
            m_Onconnect = onConnect;
            m_OnClose = onClose;
            ID = sessionID;
            var option = new STSocketOption();
            //그외 옵션도 여기서 설정
            option.SendBufferSize = Consts.TCP_CLIENT_MAX_BUFFER_SIZE;
            option.RecvBufferSize = Consts.TCP_CLIENT_MAX_BUFFER_SIZE;
            ClientSocket = new TCPClientSocket(OnConnect, option, OnSend, OnReceive, OnClose, OnError);
        }

        public bool ConnectEventAsync(string ipAddr, int port)
        {
            return ClientSocket.ConnectEventAsync(ipAddr, port);
        }

        public Task<bool> ConnectAsync(IPEndPoint ipEndPoint)
        {
            return ClientSocket.ConnectAsync(ipEndPoint);
        }

        public bool Connect(IPEndPoint ipEndPoint)
        {
            return ClientSocket.Connect(ipEndPoint);
        }

        private void OnConnect()
        {
            m_Onconnect.Invoke();
        }

        private void OnSend(int byteSent)
        {

        }

        private void OnReceive(MemoryStream stream)
        {
            DispatchManagerCtx.Dispatch(this, stream);
        }

        private void OnClose(DisconnectReason reason)
        {
            m_OnClose.Invoke();
        }

        private void OnError(Exception? ex, SocketError e)
        {

        }

        public SendResult SendLink(params IMsg[] msgs)
        {
            if (IsRunning() == false)
            {
                return SendResult.NotConnect;
            }
            return ClientSocket.Send(msgs);
        }

        public bool SendLoopback(IMsg msg)
        {
            return true;
        }

        public bool IsRunning()
        {
            return ClientSocket.IsRunning();
        }

        public void BeginClose(DisconnectReason reason)
        {
            ClientSocket.Close(reason);
        }

        /// <summary>
        /// 즉각 소켓을 닫는다.
        /// </summary>
        /// <param name="reason"></param>
        public void Close(DisconnectReason reason)
        {
            ClientSocket.CloseInternal(reason);
        }

        public void SetContext(IContext ctx)
        {
            //ERROR 생성자에서 이미 할당했다.
        }

        public SendResult SendError<TResponse>(MsgErrorCode errorCode) where TResponse : IResMsg, new()
        {
            var response = new TResponse();
            response.errorCode = errorCode;

            return SendLink(response);
        }

        public IPEndPoint? GetRemoteEndPoint()
        {
            return ClientSocket.GetRemoteEndPoint();
        }

        public SendResult SendError<TResponse>(MsgErrorCode errorCode, long targetPacketUID) where TResponse : ITargetResMsg, new()
        {
            var response = new TResponse();
            response.errorCode = errorCode;
            response.targetPacketUID = targetPacketUID;

            return SendLink(response);
        }

        public void Dispose()
        {
            ClientSocket.Dispose();
        }
    }
}
