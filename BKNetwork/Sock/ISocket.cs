using System.Net.Sockets;
using BKNetwork.ConstEnum;
using BKProtocol;

namespace BKNetwork.Sock
{
    public delegate void OnSendPacket(int byteWritten);
    public delegate void OnRecvPacket(MemoryStream stream);
    public delegate void OnSocketError(Exception? ex = null, SocketError e = SocketError.Success);

    //Only TCP
    public delegate void OnTCPClientClose(DisconnectReason reason);

    public struct STSocketOption
    {
        public bool DontLinger;
        public bool KeepAlive;
        public int TCPKeepAliveInterval;
        public int TCPKeepAliveRetryCount;
        public bool TCPNoDelay;
        public bool DualIPMode;
        public bool ReuseAddr;
        public int RecvBufferSize;
        public int SendBufferSize;
    }

    public abstract class ISocket : IDisposable
    {
        protected Socket m_Socket;
        protected OnSocketError m_OnSocketError;
        protected STSocketOption m_SocketOption;
        protected bool m_Disposed;

        protected ISocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, STSocketOption option, OnSocketError e)
        {
            m_Socket = new Socket(addressFamily, socketType, protocolType);
            m_OnSocketError = e;
            m_SocketOption = option;
        }

        protected ISocket(Socket socket, STSocketOption option, OnSocketError e)
        {
            m_Socket = socket;
            m_SocketOption = option;
            m_OnSocketError = e;
        }

        public abstract void SetSocketOption(STSocketOption option);

        public abstract void Dispose();

        protected abstract void Dispose(bool disposing);

        public abstract SendResult Send(IMsg[] msgs);
    }
}
