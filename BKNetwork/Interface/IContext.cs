using BKNetwork.ConstEnum;
using BKProtocol;

namespace BKNetwork.Interface
{
    public interface IContext : IDisposable
    {
        int GetSessionID();
        int ServerID { get; }
        long GetUserUID();
        void CloseAsync(DisconnectReason reason);
        SendResult Send(IMsg msg);
        void Update();
        SendResult SendError<TResponse>(MsgErrorCode errorCode)
            where TResponse : IResMsg, new();
        SendResult SendError<TResponse>(MsgErrorCode errorCode, long targetPacketUID)
            where TResponse : ITargetResMsg, new();
    }
}
