using System.Collections.Concurrent;
using BKServerBase.ConstEnum;
using BKServerBase.Interface;
using BKServerBase.Logger;
using BKServerBase.Messaging;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKCommonComponent.Redis.Detail;
using BKGameServerComponent.Actor;
using BKGameServerComponent.MsgRegister;
using BKGameServerComponent.Network;
using BKNetwork.ConstEnum;
using BKNetwork.Interface;
using BKProtocol;
using BKProtocol.C2G;

namespace BKGameServerComponent.Session
{
    internal partial class SessionManager : IRunnable, IJobActor<SessionManager>
    {
        private readonly JobDispatcher m_JobDispatcher;
        private readonly ConcurrentDictionary<SessionID, IContext> m_Contexts = new ConcurrentDictionary<SessionID, IContext>();
        private BackendDispatcher? m_BackendDispatcher;
        private long m_RunnableID;
        private ThreadCoordinator m_ThreadCoordinator;
        private PubSubDispatchManager m_PubsubDispatcherManager = new PubSubDispatchManager();


        public SessionManager(ThreadCoordinator threadCoordinator)
        {
            m_JobDispatcher = new JobDispatcher(false);
            m_ThreadCoordinator = threadCoordinator;
        }

        public RunnableType RunnableType { get; private set; } = RunnableType.System;
        public int ThreadWorkerID { get; private set; } = ConstEnum.Consts.SESSION_MANAGER_RUNNABLE_ID;
        public SessionManager Owner => this;

        public void Initialize()
        {
            m_RunnableID = m_ThreadCoordinator.MakeRunnableID();
            m_ThreadCoordinator.AddRunnable(this);
            m_BackendDispatcher = new BackendDispatcher(
                ownerType: BKServerBase.ConstEnum.TaskSequencerMode.TickPump,
                name: "SessionManager",
                IsImmediateMode: true,
                objectID: 0);

            //NOTE pubsub 메시지 핸들링용 디스패처 등록
            RegisterPubsubDispatch();
        }

        public bool TryGetServerID(SessionID sessionID, out int serverID)
        {
            serverID = 0;
            if (m_Contexts.TryGetValue(sessionID, out var context) is false)
            {
                return false;
            }

            serverID = context.ServerID;
            return true;
        }

        public void Add(IContext context)
        {
            if (m_Contexts.TryAdd(context.GetSessionID(), context) == false)
            {
                CoreLog.Normal.LogError($"duplicated session id, failed to add session, sessionId: {context.GetSessionID()}");
                return;
            }
        }

        public void Remove(SessionID sessionId, DisconnectReason reason)
        {
            if (m_Contexts.TryRemove(sessionId, out var context) == false)
            {
                GameNetworkLog.Normal.LogError($"session does not exist, failed to remove session, sessionId: {sessionId}");
                return;
            }
            
            context.CloseAsync(reason);
            context.Dispose();
        }

        public CustomTask PostTaskCloseSession(SessionID sessionID, DisconnectReason reason, IActor taskOwner)
        {
            return this.PostTask(self =>
            {
                Remove(sessionID, reason);
            }, taskOwner);
        }

        public void PostCloseSession(SessionID sessionID, DisconnectReason reason)
        {
            this.Post(self =>
            {
                Remove(sessionID, reason);
            });
        }

        public int PickServerID()
        {
            if (m_Contexts.Count is 0)
            {
                CoreLog.Critical.LogError($"PickSessionID is failed, context's count is 0");
                return -1;
            }

            int pickIndex = RandomPicker.Next(m_Contexts.Count);
            var ctx = m_Contexts.ElementAt(pickIndex);

            return ctx.Value.ServerID;
        }
        
        public void OnUpdate()
        {
            m_JobDispatcher.RunAction();
            m_BackendDispatcher?.ExecuteResponseHandlers();
            foreach (var context in m_Contexts)
            {
                context.Value.Update();
            }
        }

        public void OnPostUpdate()
        {
            
        }

        public long GetID()
        {
            return m_RunnableID;
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
            
        }
        
        public BackendDispatcher GetBackendDispatcher()
        {
            if (m_BackendDispatcher == null)
            {
                throw new Exception("m_BackendDispatcher not initialized");
            }
            return m_BackendDispatcher;
        }

        public void Dispose()
        {
            m_JobDispatcher.Dispose();
        }

        public void InvokeTargetPubsubMsgRouting(SessionID sessionID, IPubsubMsg msg)
        {
            var playerActor = GetPlayerActor(sessionID);
            if (playerActor is null || playerActor is not Player player)
            {
                return;
            }

            m_PubsubDispatcherManager.Dispatch(player, msg);
        }

        public CustomTask<bool> SendToServerNodeAsync(int serverID, IMsg msg, IActor taskOwner)
        {
            return this.PostTask(self =>
            {
                return SendToServerNode(serverID, msg);
            }, taskOwner);
        }

        public JobDispatcher GetDispatcher()
        {
            return m_JobDispatcher;
        }

        private IPlayerActor? GetPlayerActor(SessionID sessionID)
        {
            if (m_Contexts.TryGetValue(sessionID, out var context) == false)
            {
                return null;
            }

            if (context is not SessionContext sessionContext || sessionContext.Player == null)
            {
                return null;
            }

            return sessionContext.Player;
        }
        private IEnumerable<IPlayerActor?> GetPlayerActors()
        {
            return m_Contexts.Where(e =>
            {
                if (e.Value is not SessionContext sessionContext)
                {
                    return false;
                }

                if (sessionContext.Player is null)
                {
                    return false;
                }

                return true;
            })
            .Cast<SessionContext>()
            .Select(e => e.Player);
        }

        private bool SendToServerNode(int serverID, IMsg msg)
        {
            var context = m_Contexts.Values.Where(e => e.ServerID == serverID).FirstOrDefault();
            if (context is null)
            {
                CoreLog.Critical.LogError($"SendToNodeByServerID failed, serverID is invalid, serverID: {serverID}");
                return false;
            }
            var sendResult = context.Send(msg);
            return sendResult is SendResult.Success;
        }
    }
}
