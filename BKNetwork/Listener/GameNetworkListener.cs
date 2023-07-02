using BKServerBase.Config;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKNetwork.ConstEnum;
using BKNetwork.Dispatch.Manager;
using BKNetwork.Interface;
using BKNetwork.Server;

namespace BKNetwork.Listener
{
    //NOTE 다수개의 게임 네트워크 연결 관리를 한군데서 몰아서 하기 위한 최상위 네트워크 모듈
    public class GameNetworkListener
    {
        private readonly TCPServer m_Server;
        public readonly string IpAddress = string.Empty;
        public readonly int Port;

        public GameNetworkListener(
            int port,
            ServerDispatchManager dispatchManager, 
            OnSessionCreate onCreate, 
            OnSessionClose onDestroy, 
            OnSessionError onError)
        {
            Port = port;
            m_Server = new TCPServer(dispatchManager, Port, onCreate, onDestroy, onError);

            CommonUtil.GetNicsV4(out var ipv4List);
            IpAddress = ipv4List.First();
        }

        public void Initialize()
        {
            m_Server.Start();
        }

        public bool OnUpdate(double delta)
        {
            return true;
        }

        public bool Shutdown()
        {
            return true;
        }

        public (string ip, int port) GetMachineAddress()
        {
            //TODO 외부 IP(게임서버 앞에 위치하는 LB)를 노출시켜야 하는 경우에 대해서는 Config에 추가한 후 대응한다.
            return (IpAddress, Port);
        }
    }
}
