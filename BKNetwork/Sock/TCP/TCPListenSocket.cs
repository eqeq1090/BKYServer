using System.Net;
using System.Net.Sockets;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKNetwork.ConstEnum;
using BKNetwork.Sock;
using BKProtocol;

namespace BKNetwork.Sock.TCP
{
    public delegate void OnAcceptEvent(SocketAsyncEventArgs e);
    public delegate void OnListenError(Exception ex);

    public class TCPListenSocket : ISocket
    {
        private AtomicFlag m_Running = new AtomicFlag(true);

        private SocketAsyncEventArgs m_AcceptEventArgs = new SocketAsyncEventArgs();
        private OnAcceptEvent m_OnAccept;
        private OnListenError m_ListenError;
        private int m_Port;

        public TCPListenSocket(int port, STSocketOption option, OnAcceptEvent onAcceptEvent, OnSocketError onSocketError, OnListenError onListenError)
            : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, option, onSocketError)
        {
            m_Port = port;
            m_OnAccept += onAcceptEvent;
            m_ListenError += onListenError;
        }

        public int Port => m_Port;

        private void OnAccept(object? sender, SocketAsyncEventArgs e)
        {
            if (m_Running.IsOn == false)
            {
                return;
            }
            m_OnAccept.Invoke(e);
        }

        public void Listen()
        {
            try
            {
                m_AcceptEventArgs.Completed += OnAccept!;
                m_Socket.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), m_Port));
                m_Socket.Listen(Consts.TCP_LISTEN_BACKLOG);
            }
            catch (Exception ex)
            {
                CoreLog.Critical.LogError(ex);
                m_ListenError(ex);
            }
        }

        public void StartAccept()
        {
            m_AcceptEventArgs.AcceptSocket = null;
            if (m_Socket.AcceptAsync(m_AcceptEventArgs) == false)
            {
                m_OnAccept(m_AcceptEventArgs);
            }
        }

        public bool IsRunning()
        {
            return m_Running.IsOn;
        }

        public override void SetSocketOption(STSocketOption option)
        {
            if (option.TCPNoDelay == true)
            {
                m_Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            }
            if (option.DontLinger == true)
            {
                m_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            }
            if (option.ReuseAddr == true)
            {
                m_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
        }

        public void Shutdown()
        {
            m_Running.Off();
            m_Socket.Close();
        }

        public override void Dispose()
        {
            if (true == m_Disposed)
            {
                return;
            }
            Dispose(true);
            m_Disposed = true;
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            Shutdown();
        }

        public override SendResult Send(IMsg[] msgs)
        {
            CoreLog.Critical.LogFatal("Invalid Send Func Call. It tried on ListenSocket");
            return SendResult.Undefined;
        }
    }
}
