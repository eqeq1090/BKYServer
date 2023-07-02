using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKNetwork.ConstEnum
{
    public enum SendResult
    {
        Success,
        InvalidSocketType,
        NotConnect,
        InvalidSize,
        Undefined,
        LoopBackFailed
    }

    public enum TCPClientConnectState
    {
        NotConnected,
        Connecting,
        Connected,
        Reconnecting
    }

    enum ePrometheusGaugeKind
    {
        SendBytes,
        RecvBytes,
        SendCount,
        RecvCount,
        SendCallbackCount,
        SendDeferredCount,
        Max
    }

    public enum RedisConnectorType
    {
        Pubsub,
        Command
    }

    public enum CommandRedisContentsType
    {
        Presense,
        Team,
        MatchData,
        Rank,
        Session
    }

    public enum ServiceDiscoveryInfoType
    {
        Invalid,
        c2g,
        g2a, //APIServer
        m2g,
        gl2g
    }
}
