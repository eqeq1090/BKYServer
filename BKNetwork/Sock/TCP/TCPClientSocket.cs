using System.Net;
using System.Net.Sockets;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKNetwork.ConstEnum;
using BKProtocol;

namespace BKNetwork.Sock.TCP
{
    public delegate void OnConnect();
    public delegate void OnClose();

    public class TCPClientSocket : TCPAcceptSocket, IDisposable
    {
        private OnConnect m_OnConnect;

        public TCPClientSocket(OnConnect onConnect, STSocketOption option, OnSendPacket onSend, OnRecvPacket onRecv, OnTCPClientClose onClose, OnSocketError onSocketError)
            : base(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), option, onSend, onRecv, onClose, onSocketError)
        {
            m_OnConnect += onConnect;
        }

        public bool ConnectEventAsync(string targetIPAddr, int targetPort)
        {
            try
            {
                m_RefCount.Increment(RefCountReason.SocketConnect);
                var targetIP = Dns.GetHostAddresses(targetIPAddr)[0];
                var endPoint = new IPEndPoint(targetIP, targetPort);

                var args = new SocketAsyncEventArgs();
                args.Completed += ConnectCallback;
                args.RemoteEndPoint = endPoint;
                if (m_Socket.ConnectAsync(args) == false)
                {
                    ConnectCallback(null, args);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public async Task<bool> ConnectAsync(IPEndPoint ipEndPoint)
        {
            try
            {
                m_RefCount.Increment(RefCountReason.SocketConnect);
                await m_Socket.ConnectAsync(ipEndPoint);

                StartReceive();
                m_OnConnect();

                return true;
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"ConnectAsync failed for {ipEndPoint.ToString()}\n message: {e.ToString()}");
                return false;
            }
        }

        public bool Connect(IPEndPoint ipEndPoint)
        {
            try
            {
                m_RefCount.Increment(RefCountReason.SocketConnect);
                m_Socket.Connect(ipEndPoint);

                StartReceive();
                m_OnConnect();

                return true;
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError(e.ToString());
                return false;
            }
        }

        private void ConnectCallback(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                e.Completed -= ConnectCallback;
                e.Dispose();

                if (e.SocketError != SocketError.Success)
                {
                    m_OnSocketError(e: e.SocketError);
                    return;
                }

                StartReceive();
                m_OnConnect();
            }
            catch (Exception ex)
            {
                m_OnSocketError(ex);
                CloseInternal(DisconnectReason.Undefined);
            }
        }

        public override void SetSocketOption(STSocketOption option)
        {
            
        }
    }
}
