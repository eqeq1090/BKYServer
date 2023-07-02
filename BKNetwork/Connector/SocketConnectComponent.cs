using BKServerBase.Component;
using BKServerBase.Threading;
using System.Collections.Concurrent;
using BKNetwork.Session;
using BKNetwork.Dispatch.Manager;

namespace BKNetwork.Connector
{
    //다중 클라이언트 붙인걸 관리하는 용도로 씀
    public class SocketConnectComponent : IComponent
    {
        private readonly ConcurrentDictionary<int, TCPClientSession> m_Clients = new ConcurrentDictionary<int, TCPClientSession>();
        private EventTimer? m_TimerTaskEvent;
        public ServerDispatchManager DispatchManagerCtx { get; private set; } = new ServerDispatchManager();
        public (bool success, OnComponentInitializedHandler? InitDoneFunc) Initialize()
        {
            m_TimerTaskEvent = new EventTimer();
            return (true, null);
        }

        public void TerminateAllConnection()
        { }

        public bool OnUpdate(double delta)
        {
            return true;
        }

        public bool Shutdown()
        {
            return true;
        }
    }
}
