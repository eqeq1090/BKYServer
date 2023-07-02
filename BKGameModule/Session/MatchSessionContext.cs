using BKServerBase.ConstEnum;
using BKServerBase.Interface;
using BKServerBase.Logger;
using BKServerBase.Messaging;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKNetwork.ConstEnum;
using BKNetwork.Interface;
using BKProtocol;

namespace BKGameServerComponent.Session
{
    internal class MatchSessionContext : IContext, IJobActor<MatchSessionContext>, IRunnable
    {
        private readonly JobDispatcher m_JobDispatcher;
        private readonly ISession m_Session;
        private int m_ServerID;

        public MatchSessionContext(ISession session)
        {
            m_JobDispatcher = new JobDispatcher(false);
            m_Session = session;
            m_Session.SetContext(this);
        }

        public RunnableType RunnableType => RunnableType.Zone;
        public int ThreadWorkerID { get; private set; }
        public MatchSessionContext Owner => this;
        public int ServerID => m_ServerID;

        public void SetServerID(int serverID)
        {
            m_ServerID = serverID;
        }

        public void PostHandler(Action action)
        {
            this.Post(self =>
            {
                action.Invoke();
            });
        } // 패킷 receive 순서 보장용으로 사용.

        public SendResult Send(IMsg msg)
        {
            return m_Session.SendLink(msg);
        }

		public void Dispose()
		{
            m_ServerID = 0;
            m_JobDispatcher.Dispose();
        }

        public JobDispatcher GetDispatcher()
        {
            return m_JobDispatcher;
        }

        public long GetUserUID()
        {
            return 0;
        }

        public void OnUpdate()
        {
            m_JobDispatcher.RunAction();
        }

        public void OnPostUpdate()
        {
        }

        public long GetID()
        {
            return 0;
        }

        public int GetScore()
        {
            return 0;
        }

        public void SetBlocked(bool flag)
        {
        }

        public void SetThread(Thread thread, int threadWorkerID)
        {
            ThreadWorkerID = threadWorkerID;
        }

        public int GetSessionID()
        {
            return m_Session.ID;
        }

        public void CloseAsync(DisconnectReason reason)
        {
            m_Session.BeginClose(reason);
        }

        public SendResult SendError<TResponse>(MsgErrorCode errorCode) where TResponse : IResMsg, new()
        {
            CoreLog.Critical.LogError($"SendError => errorCode: {errorCode}");
            return m_Session.SendError<TResponse>(errorCode);
        }

        public SendResult SendError<TResponse>(MsgErrorCode errorCode, long playerUID) where TResponse : ITargetResMsg, new()
        {
            CoreLog.Critical.LogError($"SendError => playerUID: {playerUID}, errorCode: {errorCode}");
            return m_Session.SendError<TResponse>(errorCode, playerUID);
        }

        public void Update()
        {

        }
    }
}
