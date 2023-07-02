using System.IO;
using BKNetwork.Interface;
using BKProtocol;

namespace BKNetwork.Dispatch
{
    public delegate void OnDispatchEventHandler<T>(T msg, IContext ctx, bool passFlag)
        where T : IMsg, new();
    public delegate void OnTargetDispatchEventHandler<T>(T msg, IContext ctx)
        where T : IMsg, new();
    public delegate void OnClientDispatchEventHandler<T>(IContext ctx, T msg)
        where T : IMsg, new ();
    public delegate void OnPreDispatchEventHandler(IMsg msg, IContext ctx);
    public delegate void OnPreSendEventHandler(IMsg msg, IContext ctx);

    public interface IDispatcher
    {
        bool Fetch(IContext context, MemoryStream memoryStream);
        bool Fetch(IContext context, IMsg msg);
    }
}
