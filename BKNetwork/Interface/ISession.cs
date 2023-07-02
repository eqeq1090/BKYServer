using System.Net;
using BKNetwork.ConstEnum;
using BKProtocol;

namespace BKNetwork.Interface
{
    public delegate void OnSessionCreate(ISession session);
    public delegate void OnSessionClose(IContext context, DisconnectReason reason);
    public delegate void OnSessionError(IContext context, Exception ex);

    public interface ISession
    {
        int ID { get; }
        IContext? Context { get; }
        SendResult SendLink(params IMsg[] msgs);
        SendResult SendError<TResponse>(MsgErrorCode errorCode)
            where TResponse : IResMsg, new();
        SendResult SendError<TResponse>(MsgErrorCode errorCode, long playerUID)
            where TResponse : ITargetResMsg, new();
        bool SendLoopback(IMsg msg);
        void SetContext(IContext ctx);
        void BeginClose(DisconnectReason reason);
        void Close(DisconnectReason reason);
        IPEndPoint? GetRemoteEndPoint();
    }
}