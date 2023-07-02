using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.ConstEnum
{
    public enum TCPConnectorState
    {
        NONE,
        CONNECTING,
        RECONNECTING,
        CONNECTED,
        DISCONNECTED,
    }
    
    public enum SystemEventType
    {
        TickExceed,
    }

    public enum eChannelManagerMode
    {
        ThreadCoordinator,
        TimerWheel
    }

    public enum eThreadCoordinatorMode
    {
        RoundRobin,
    }

    public enum TimerWheelTickType
    {
        Sleep,
        SpinWait
    }

    public enum CompressionType
    {
        None,
        LZ4,
        zlib,
    }

    public enum ThreadWorkerUpKeep
    {
        Invalid,
        LOW,
        Mid,
        High,
        Blocked,
    }

    public enum eThreadWorkerState
    {
        Static,
        Free,
        Normal,
        Busy,
        Stopped,
    }

    public enum RunnableType
    {
        Pool,
        System,
        Zone,
        Bot
    }

    public enum TaskSequencerMode
    {
        TickPump,
        QueuePump
    }

    public enum ENumberPackType
    {
        Zero = 0, // 0 bit
        Under_32 = 1,// 5 bit
        Under_64 = 2,// 6 bit
        Under_256 = 3,// 8 bit
        Under_1024 = 4,// 10 bit
        Under_4096 = 5,// 12 bit
        Under_16384 = 6,// 14 bit
        Under_65535 = 7,// 16 bit
    };

    public enum BKSchemaType
    {
        bk_global_master,
        bk_player_shard,
    }

    public enum BKRedisDataType
    {
        Invalid,
        session,
        presence,
    }

    public enum AWSEndPointType
    {
        USEast1,
        USWest1,
        Hongkong,
        Tokyo,
        Seoul,
        Singapore,
    }

    public enum ServerProfile
    {
        Local,
        Dev,
        QA,
        Product
    }

    public enum StageServerType
    {
        Invalid,
        QANormal,
        TimeWarp,
        Hotfix,
        Review,
        PublishQA
    }

    public enum RedisServiceType
    {
        Invalid,
        Pubsub,
        Command
    }

    public enum RedisBindServerType
    {
        Invalid,
        APIServer,
        GameServer,
        MatchServer
    }

    public enum ServerMode
    {
        AllInOne,
        GameServer,
        APIServer,
        APIUnitTest,
        DiscoveryServer,
        Testor
    }

    public enum BehaviorType
    {
        Login,
        Logout,
    }
    public enum GroupNum
    {
        BirdLetter,
        VNG,
        Mobirix,
        Krafton,
        Com2us,
        NHN,
        Nexon,
        Netmarble,
        Gravity,
        BiliBili,
        Smilegate,
        BabilGames,
        Miniclip,
        KakaoGames,
    }
}
