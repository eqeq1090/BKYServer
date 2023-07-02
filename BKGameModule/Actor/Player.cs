using StackExchange.Redis;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Messaging;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKCommonComponent.Redis.Detail;
using BKDataLoader.MasterData;
using BKGameServerComponent.Actor.Detail;
using BKGameServerComponent.Channel;
using BKGameServerComponent.Controller;
using BKGameServerComponent.Controller.Detail;
using BKGameServerComponent.MsgRegister;
using BKGameServerComponent.Network;
using BKGameServerComponent.Session;
using BKNetwork.ConstEnum;
using BKProtocol;
using BKProtocol.Enum;
using BKProtocol.G2A;

namespace BKGameServerComponent.Actor
{
    internal interface IPlayer
    {
        long PlayerUID { get; }
        long ChannelID { get; }
        LocationType Location { get; }
        void SetLocation(LocationType location);
        void SetPresenceData(PresenceData data);
        void SendToMe(IMsg msg);
        SendResult SendError<T>(MsgErrorCode errorCode)
            where T : IResMsg, new();
    }

    internal interface IPlayerActor : IPlayer, IJobActor<Player>
    {
        void PostHandler<T>(OnPlayerDispatchEventHandler<T> handler, T msg)
            where T : IMsg, new();

        string BackendSessionID { get; }
        PresenceData PresenceData { get; }
    }

    internal partial class Player : IDisposable, IPlayerActor
    {
        private readonly SessionContext m_SessionContext;
        private readonly BackendDispatcher m_BackendDispatcher;
        private readonly AtomicFlag m_Disposed = new AtomicFlag(false);
        private readonly IControllerCollection m_Controllers;
        private readonly long m_PlayerUID;

        private readonly CommonChannel Channel;

        private string m_PlayerName = string.Empty;


        private string m_BackendSessionID = string.Empty;

        private PresenceData m_PresenceData;

        public readonly List<int> m_OwnedTimerIDs = new List<int>();
        public readonly int ObjectID;

        public Player(
            int objectID,
            SessionContext context,
            CommonChannel channel,
            BackendDispatcher backendDispatcher,
            string sessionID,
            long playerUID)
        {
            m_BackendSessionID = sessionID;
            ObjectID = objectID;
            m_SessionContext = context;
            m_PlayerUID = playerUID;
            m_BackendDispatcher = backendDispatcher;
            Channel = channel;

            m_PresenceData = new PresenceData()
            {
                PlayerUID = playerUID,
                Location = LocationType.Lobby,
            };

            m_Controllers = new ControllerCollection(this);

            ObjectCounter<Player>.Increment();
        }

        public long PlayerUID => m_PlayerUID;
        public long ChannelID => Channel.GetID();
        public LocationType Location => m_PresenceData.Location;
        public PresenceData PresenceData => m_PresenceData;
        public string BackendSessionID => m_BackendSessionID;

        public void Initialize(PlayerInfo playerInfo)
        {
            ApplyPlayerInfo(playerInfo);
        }


        public void ApplyPlayerInfo(PlayerInfo info)
        {
            m_PlayerName = info.name;
        }

        ~Player()
        {
            Dispose(false);
        }

        public Player Owner => this;

        public async CustomTask OnRemoveAsync()
        {
            foreach (var id in m_OwnedTimerIDs)
            {
                GameServerComponent.Instance.PostCancelTimerEvent(id);
            }

            var apiRes = await SendRequestAsync<APILogoutRes>(new APILogoutReq()
            {
                playerUID = m_PlayerUID,
            });
            if (apiRes.errorCode is not MsgErrorCode.Success)
            {
                ContentsLog.Critical.LogError($"Logout failed, playerUID: {m_PlayerUID}, errorCode: {apiRes.errorCode}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            ObjectCounter<Player>.Decrement();
            if (m_Disposed.IsOn == true || disposing == false)
            {
                return;
            }
            if (m_Disposed.On() == false)
            {
                return;
            }
        }

        public void PostHandler<T>(OnPlayerDispatchEventHandler<T> handler, T msg)
            where T : IMsg, new()
        {
            this.Post(self =>
            {
                handler.Invoke(this, msg);
            });
        }

        public void SendToMe(IMsg msg)
        {
            m_SessionContext.Send(msg);
        }

        public SendResult SendError<T>(MsgErrorCode errorCode)
            where T : IResMsg, new()
        {
            ContentsLog.Critical.LogError($"SendError, errorCode: {errorCode}");

            return m_SessionContext.m_Session.SendError<T>(errorCode);
        }

        public CustomTask<TResponse> SendRequestAsync<TResponse>(IMsg req, string file = "", int line = 0)
            where TResponse : IAPIResMsg, new()
        {
            var caller = "";
            CustomTask<TResponse> task = new CustomTask<TResponse>(caller);
            //이런저런 체크를 한다.
            //사망중인지, 백엔드 전송이 막힌 채널인지 등
            var result = m_BackendDispatcher?.TaskRequest(Channel.GetID(), PlayerUID, req, (apiAns, success) =>
            {
                if (apiAns is not TResponse response)
                {
                    CoreLog.Critical.LogError($"Response data is invalid");
                    return;
                }

                if (success is false)
                {
                    task.SetResult(new TResponse()
                    {
                        errorCode = MsgErrorCode.ApiErrorNotDefined
                    });
                    return;
                }

                task.SetResult(response);
            }, m_BackendSessionID);

            if (result == false)
            {
                //에러 리턴
                task.SetResult(new TResponse()
                {
                    errorCode = MsgErrorCode.InvalidErrorCode
                });
            }
            return task;
        }

        public JobDispatcher GetDispatcher()
        {
            return Channel.GetDispatcher();
        }

        public void SetLocation(LocationType location)
        {
            m_PresenceData.Location = location;

            var redisKey = RedisKeyGroup.MakePresenceKey(m_PlayerUID);
            GameServerComponent.Instance.InvokeSaveRedisInfo(
                BKRedisDataType.presence,
                redisKey,
                m_PresenceData,
                expiry: null,
                this,
                flags: CommandFlags.FireAndForget)
            .HandleError();
        }
        public void SetPresenceData(PresenceData data)
        {
            m_PresenceData = data;
        }

    }
}
