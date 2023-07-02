using System.Net.Sockets;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKNetwork.Dispatch.Manager;
using BKNetwork.Interface;
using BKNetwork.Session;
using BKNetwork.Sock;
using BKNetwork.Sock.TCP;

namespace BKNetwork.Server
{
    public class TCPServer : IDisposable
    {
        private int m_SessionIDGenerator = 0;

        private TCPListenSocket m_ListenSocket;

        private OnSessionCreate m_OnSessionCreate;
        private OnSessionClose m_OnSessionClose;
        private OnSessionError m_OnSessionError;

        public ServerDispatchManager m_DispatchManager;

        private STSocketOption m_SocketOption;

        private AtomicFlag m_Running = new AtomicFlag(true);

        public TCPServer(ServerDispatchManager dispatcherManager, int port, OnSessionCreate onSessionCreate, OnSessionClose onSessionClose, OnSessionError onSessionError)
        {
            m_SocketOption = new STSocketOption()
            {
                RecvBufferSize = ConstEnum.Consts.TOTAL_RECV_BUFFER_SIZE,
                SendBufferSize = ConstEnum.Consts.TOTAL_SEND_BUFFER_SIZE,
                ReuseAddr = true
            };
            //그외 옵션도 여기서 설정
            m_ListenSocket = new TCPListenSocket(port, m_SocketOption, OnAccept, OnError, OnListenError);
            m_OnSessionCreate = onSessionCreate;
            m_OnSessionClose = onSessionClose;
            m_OnSessionError = onSessionError;
            m_DispatchManager = dispatcherManager;
        }

        public void Shutdown()
        {
            m_Running.Off();
            m_ListenSocket.Shutdown();
        }

        public void Start()
        {
            m_ListenSocket.Listen();
            m_ListenSocket.StartAccept();

            CoreLog.Normal.LogInfo($"Listening port: {m_ListenSocket.Port}");
        }

        private void OnAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && m_Running.IsOn == true)
            {
                // Create a new session to register
                var session = new TCPServerSession(GetNewSessionID(), m_DispatchManager, e.AcceptSocket!, m_SocketOption.RecvBufferSize, m_SocketOption.SendBufferSize, m_OnSessionClose, m_OnSessionError);

                m_OnSessionCreate.Invoke(session);
                session.StartReceive();
            }
            else
            {
                OnError(null, e.SocketError);
            }
            m_ListenSocket.StartAccept();
        }

        private int GetNewSessionID()
        {
            return Interlocked.Increment(ref m_SessionIDGenerator);
        }

        private void OnError(Exception? ex, SocketError e)
        {
            if (ex != null)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine(e.ToString());
        }

        private void OnListenError(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        public void Dispose()
        {

        }
    }
}
