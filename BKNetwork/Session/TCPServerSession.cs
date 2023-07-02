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
    public class TCPServerSession : ISession
    {
        public int ID { get; init; }

        public ServerDispatchManager DispatchManagerCtx { get; private set; }

        public TCPAcceptSocket AcceptSocket { get; private set; }

        private OnSessionClose m_OnSessionClose;
        private OnSessionError m_OnSessionError;

        public IContext? Context { get; private set; }

        public TCPServerSession(int sessionID, ServerDispatchManager dispatcherManager, Socket socket, int recvBufferSize, int sendBufferSize, OnSessionClose onSessionClose, OnSessionError onSessionError)
        {
            ID = sessionID;
            DispatchManagerCtx = dispatcherManager;
            var option = new STSocketOption()
            {
                SendBufferSize = sendBufferSize,
                RecvBufferSize = recvBufferSize
            };
            AcceptSocket = new TCPAcceptSocket(socket, option, OnSend, OnReceive, OnClose, OnSocketError);
            m_OnSessionClose += onSessionClose;
            m_OnSessionError += onSessionError;
        }

        public void StartReceive()
        {
            AcceptSocket.StartReceive();
        }

        private void OnSocketError(Exception? ex, SocketError e)
        {
            //소켓 에러는 에러로 출력
            //소켓 에러 중 일부는 연결 차단을 요하는 경우가 있으므로 해당 케이스만 세션 핸들러에 넘겨서 액터 정리를 요함
            if (ex != null)
            {
                m_OnSessionError.Invoke(Context!, ex);
            }
        }

        private void OnClose(DisconnectReason reason)
        {
            m_OnSessionClose.Invoke(Context!, reason);
        }

        private void OnSend(int byteSent)
        {

        }

        private void OnReceive(MemoryStream stream)
        {
            if (DispatchManagerCtx.Dispatch(this, stream) == false)
            {
                BeginClose(DisconnectReason.SessionError);
            }
        }

        public void BeginClose(DisconnectReason reason)
        {
            AcceptSocket.Close(reason);
        }

        public void Close(DisconnectReason reason)
        {
            AcceptSocket.CloseInternal(reason);
        }

        public bool IsRunning()
        {
            return AcceptSocket.IsRunning();
        }

        public SendResult SendLink(params IMsg[] msgs)
        {
            if (IsRunning())
            {
                //TODO 혹시 전송전에 공용 PRESEND 딜리게이트 호출할 생각이면 디스패처매니저에 함수 하나 만들어서 실행
                return AcceptSocket.Send(msgs);
            }
            else
            {
                return SendResult.NotConnect;
            }
        }

        public bool SendLoopback(IMsg msg)
        {
            return DispatchManagerCtx.LoopbackLink(this, msg);
        }

        public void SetContext(IContext context)
        {
            Context = context;
        }

        public SendResult SendError<TResponse>(MsgErrorCode errorCode) where TResponse : IResMsg, new()
        {
            var response = new TResponse();
            response.errorCode = errorCode;

            return SendLink(response);
        }

        public IPEndPoint? GetRemoteEndPoint()
        {
            return AcceptSocket.GetRemoteEndPoint();
        }

        public SendResult SendError<TResponse>(MsgErrorCode errorCode, long playerUID) where TResponse : ITargetResMsg, new()
        {
            var response = new TResponse();
            response.errorCode = errorCode;
            response.targetPacketUID = playerUID;

            return SendLink(response);
        }
    }
}
