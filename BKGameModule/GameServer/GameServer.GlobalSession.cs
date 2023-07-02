using Prometheus.DotNetRuntime.EventListening.Parsers.Util;
using System.Threading.Tasks;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKGameServerComponent.Actor;
using BKGameServerComponent.Session;
using BKNetwork.ConstEnum;
using BKNetwork.Dispatch;
using BKNetwork.Interface;
using BKProtocol;
using BKProtocol.Pubsub;

namespace BKGameServerComponent
{
    public partial class GameServerComponent
    {
        public void OnGlobalSessionAdd(ISession session)
        {
            var remoteEndPoint = session.GetRemoteEndPoint();
            GameNetworkLog.Normal.LogInfo($"GlobalSession is connected, sessionId: {session.ID}, remoteEndPoint: {remoteEndPoint?.ToString()}");

            var sessionContext = new TeamSessionContext(session);
            m_GlobalSessionManager.Add(sessionContext);
        }

        public void OnGlobalSessionRemove(IContext context, DisconnectReason reason)
        {
            GameNetworkLog.Critical.LogError($"GlobalSession is disconnected " +
                $"sessionId: {context.GetSessionID()}",
                $"reason: {reason}");
            m_GlobalSessionManager.Remove(context.GetSessionID(), DisconnectReason.ByServer);
        }

        public void OnGlobalSessionError(IContext context, Exception ex)
        {
            GameNetworkLog.Critical.LogError($"GlobalSession occurred error sessionId: {context.GetSessionID()}\n error: {ex.ToString()}");
            m_GlobalSessionManager.Remove(context.GetSessionID(), DisconnectReason.SessionError);
        }

        internal CustomTask<T> SendToGlobalNodeAsync<T>(ITargetMsg msg, IActor taskOwner)
            where T : ITargetResMsg, new()
        {
            var teamServerID = m_GlobalSessionManager.PickServerID();
            if (teamServerID == ConstEnum.Consts.INVALID_SERVER_ID)
            {
                var errorResult = new CustomTask<T>(string.Empty);
                errorResult.SetResult(new T()
                {
                    errorCode = MsgErrorCode.CoreErrorPickServerNoedFailed
                });
                return errorResult;
            }

            return SendToGlobalNodeAsync<T>(teamServerID, msg, taskOwner);
        }

        private CustomTask<T> SendToGlobalNodeAsync<T>(int globalServerID, ITargetMsg msg, IActor taskOwner)
            where T : ITargetResMsg, new()
        {
            var resultTask = new CustomTask<T>(string.Empty);

            this.Post(async self =>
            {
                var targetPacketUID = KeyGenerator.PacketUID();
                msg.targetPacketUID = targetPacketUID;

                string timerName = $"GlobalServer:{targetPacketUID}";
                if (m_EventTimer.Exist(timerName) == true)
                {
                    taskOwner.Post(() =>
                    {
                        resultTask.SetResult(new T()
                        {
                            errorCode = MsgErrorCode.ClientErrorDuplicatedTimer
                        });
                    });
                    return;
                }
                var result = await m_GlobalSessionManager.SendToServerNodeAsync(globalServerID, msg, this);
                if (result is false)
                {
                    CoreLog.Critical.LogError($"SendToGlobalNode failed on match (NetworkErrorSendFailed), msgType: {msg.msgType}. teamServerID: {globalServerID}");

                    taskOwner.Post(() =>
                    {
                        resultTask.SetResult(new T()
                        {
                            errorCode = MsgErrorCode.NetworkErrorSendFailed
                        });
                    });
                    return;
                }

                var timerID = m_EventTimer.CreateTimerEvent(() =>
                {
                    m_GlobalServiceDispatcher.UnregisterDispatcherOnce<T>(targetPacketUID);
                    taskOwner.Post(() =>
                    {
                        if (resultTask.IsCompleted is false)
                        {
                            resultTask.SetResult(new T()
                            {
                                errorCode = MsgErrorCode.ClientErrorSendTimeout
                            });
                        }
                    });
                }, BaseConsts.ClientNodeSendTimeOut, false, timerName);

                OnTargetDispatchEventHandler<T> deleFunc = (msg, ctx) =>
                {
                    m_EventTimer.RemoveTimerEvent(timerID);

                    taskOwner.Post(() =>
                    {
                        if (resultTask.IsCompleted is false)
                        {
                            resultTask.SetResult(msg);
                        }
                    });
                };
                //원스 등록
                m_GlobalServiceDispatcher.RegisterDispatcherOnce(targetPacketUID, deleFunc);
            });

            return resultTask;
        }

        private int GetSessionID(long targetPlayerUID)
        {
            var sessionID = m_PlayerSessionDict
                .Where(e => e.Key == targetPlayerUID)
                .Select(e => e.Value)
                .FirstOrDefault();
            return sessionID;
        }
    }
}
