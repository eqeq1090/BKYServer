using StackExchange.Redis;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Messaging;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKCommonComponent.Redis.Detail;
using BKGameServerComponent.Actor;
using BKGameServerComponent.ConstEnum;
using BKGameServerComponent.Controller.Detail;
using BKGameServerComponent.Network;
using BKNetwork.ConstEnum;
using BKNetwork.Interface;
using BKProtocol;
using BKProtocol.C2G;
using BKProtocol.G2A;

namespace BKGameServerComponent.Session
{
    internal class SessionContext : IContext, IDisposable, IJobActor<SessionContext>
    {
        private IActor m_ActorOwner;
        private LoginStatus m_LoginStatus = LoginStatus.NotLogon;
        private IPlayerActor? m_Player = null;
        private BackendDispatcher m_BackendDispatcher;
        private AtomicFlag m_IsDisposed = new AtomicFlag(false);

        private long m_LastTick;
        
        public SessionContext(IActor actorOwner, ISession session, BackendDispatcher backendDispatcher)
        {
            m_ActorOwner = actorOwner;
            m_Session = session;
            m_BackendDispatcher = backendDispatcher;
            session.SetContext(this);
            ObjectCounter<SessionContext>.Increment();
            m_LastTick = TimeUtil.GetCurrentTickMilliSec();
        }

        ~SessionContext()
        {
            Dispose(false);
        }

        public ISession m_Session { get; private set; }
        public SessionContext Owner => this;
        public IPlayerActor? Player => m_Player;
        public int ServerID => 0;

        public void PostHandler<T>(OnClientSessionDispatchEventHandler<T> handler, T msg)
            where T : IMsg, new()
        {
            this.Post(self =>
            {
                handler.Invoke(this, msg);
            });
        }

        public void CloseAsync(DisconnectReason reason)
        {
            if (m_Player != null)
            {
                GameServerComponent.Instance.PostRemovePlayerAsync(m_Player.ChannelID, m_Player.PlayerUID);
            }
            if (reason == DisconnectReason.ByServer || reason == DisconnectReason.SessionExpire || reason == DisconnectReason.DuplicateConnect)
            {
                Send(new DisconnectSig()
                {
                    reason = reason
                });
            }
            m_Session.Close(reason);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            ObjectCounter<SessionContext>.Decrement();
            if (disposing is false)
            {
                return;
            }

            if (m_IsDisposed.On() is false)
            {
                return;
            }
        }
        
        public int GetSessionID()
        {
            return m_Session.ID;
        }

        public void Update()
        {
            CheckHeartBeatExpire();
        }

        public void CheckHeartBeatExpire()
        {
            if (TimeUtil.GetCurrentTickDiffMilliSec(m_LastTick) > ConstEnum.Consts.NATIVE_CLIENT_HEART_BEAT_TIMEOUT)
            {
                GameServerComponent.Instance.CloseUserSession(GetSessionID(), DisconnectReason.SessionExpire);
            }
        }

        public long GetUserUID()
        {
            return m_Player?.PlayerUID ?? 0;
        }

        public SendResult SendGroup(params IMsg[] msgs)
        {
            return m_Session.SendLink(msgs);
        }

        public SendResult Send(IMsg msg)
        {
            return m_Session.SendLink(msg);
        }

        public void HandleHeartBeat()
        {
            m_LastTick = TimeUtil.GetCurrentTickMilliSec();
            Send(new HeartBeatRes());
        }

        public void PostLogin(BKProtocol.C2G.LoginReq msg)
        {
            if (m_LoginStatus != LoginStatus.NotLogon)
            {
                //ERROR
                //중복 로그인 방지
                //동일 tcp socket으로 온 요청이므로 굳이 연결을 자를 필요는 없다.
                return;
            }

            // TODO: 로그인 이후 player를 생성
            //NOTE SessionContext에서 바로 처리를 하는것이 아니라 SessionManager의 Executor를 태우는 것은 순차성 보장 및 중복 실행 방지를 위해서이다.
            this.Post(async self =>
            {
                var req = new APILoginReq()
                {
                    accessToken = msg.accessToken,
                    DID = msg.DID,
                    HivePlayerID = (int)msg.HivePlayerID,
                    email = msg.email,
                };
                var res = await m_BackendDispatcher.RequestAsync<APILoginRes>(req, playerUID: 0, string.Empty);
                if (res.errorCode != MsgErrorCode.Success)
                {
                    m_Session.SendError<LoginRes>(res.errorCode);
                    return;
                }

                //응답 메시지 전송
                (var errorCode, m_Player) = await GameServerComponent.Instance.PostAddPlayerAsync(res.playerInfo.playerUID, this, res.playerInfo, res.sessionKey);
                if (errorCode != MsgErrorCode.Success)
                {
                    m_Session.SendError<LoginRes>(errorCode);
                    return;
                }

                if (m_Player == null)
                {
                    m_Session.SendError<LoginRes>(MsgErrorCode.ContentErrorAddPlayer);
                    return;
                }

                if (msg.quantumCodeVersion != GameServerComponent.Instance.QuantumCodeVersion)
                {
                    CoreLog.Critical.LogError($"QuantumCodeVersion is not correct, client version: {msg.quantumCodeVersion}, server version: {GameServerComponent.Instance.QuantumCodeVersion}");
                    errorCode = MsgErrorCode.CoreErrorQuantumCodeVersionNotCorrect;
                }

                m_Player.SetPresenceData(res.presenceData);

                var loginRes = new LoginRes()
                {
                    errorCode = errorCode,
                    playerInfo = res.playerInfo,
                };

                m_Session.SendLink(loginRes);
                // TODO: SessionManager와 SessionContext는 하나로 묵여 있는데 안쪽에서 SessionManager로 접근하는 순간 문제가 발생한다. (서로 행이 걸림)
            });
        }

        public JobDispatcher GetDispatcher()
        {
            return m_ActorOwner.GetDispatcher();
        }

        public SendResult SendError<TResponse>(MsgErrorCode errorCode) where TResponse : IResMsg, new()
        {
            return m_Session.SendError<TResponse>(errorCode);
        }

        public SendResult SendError<TResponse>(MsgErrorCode errorCode, long playerUID) where TResponse : ITargetResMsg, new()
        {
            return m_Session.SendError<TResponse>(errorCode, playerUID);
        }
    }
}
