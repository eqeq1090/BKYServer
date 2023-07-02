using Nest;
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
        public void OnMatchSessionAdd(ISession session)
        {
            var remoteEndPoint = session.GetRemoteEndPoint();
            GameNetworkLog.Normal.LogInfo($"MatchSession is connected, sessionId: {session.ID}, remoteEndPoint: {remoteEndPoint?.ToString()}");

            var sessionContext = new MatchSessionContext(session);
            m_MatchSessionManager.Add(sessionContext);
        }

        public void OnMatchSessionRemove(IContext context, DisconnectReason reason)
        {
            GameNetworkLog.Critical.LogError($"MatchSession is disconnected " +
                $"sessionId: {context.GetSessionID()}",
                $"reason: {reason}");
            m_MatchSessionManager.Remove(context.GetSessionID(), DisconnectReason.ByServer);
        }

        public void OnMatchSessionError(IContext context, Exception ex)
        {
            GameNetworkLog.Critical.LogError($"MatchSession occurred error sessionId: {context.GetSessionID()}\n error: {ex.ToString()}");
            m_MatchSessionManager.Remove(context.GetSessionID(), DisconnectReason.SessionError);
        }

        internal CustomTask<bool> SendToMatchNodeNoRes(int matchServerID, IMsg msg, IActor taskOwner)
        {
            return m_MatchSessionManager.SendToServerNodeAsync(matchServerID, msg, taskOwner);
        }

        internal CustomTask<T> SendToMatchNodeWithRes<T>(int matchServerID, ITargetMsg msg, IActor taskOwner)
            where T : ITargetResMsg, new()
        {
            return SendToMatchNodeInternal<T>(matchServerID, msg, taskOwner);
        }

        private CustomTask<T> SendToMatchNodeInternal<T>(int matchServerID, ITargetMsg msg, IActor taskOwner)
            where T : ITargetResMsg, new()
        {
            var resultTask = new CustomTask<T>(string.Empty);

            this.Post(async self =>
            {
                var targetPacketUID = KeyGenerator.PacketUID();
                msg.targetPacketUID = targetPacketUID;

                string timerName = $"Match:{targetPacketUID}";
                if (m_EventTimer.Exist(timerName) == true)
                {
                    CoreLog.Critical.LogError($"duplicated timer, msg: {msg.msgType}");

                    taskOwner.Post(() =>
                    {
                        resultTask.SetResult(new T()
                        {
                            errorCode = MsgErrorCode.ClientErrorDuplicatedTimer
                        });
                    });
                    return;
                }

                var prevTick = TimeUtil.GetCurrentTickMilliSec();
                CoreLog.Normal.LogInfo($"RequestToMatchNode, msgType: {msg.msgType}");

                var result = await m_MatchSessionManager.SendToServerNodeAsync(matchServerID, msg, this);
                if (result is false)
                {
                    CoreLog.Critical.LogError($"SendToMatchNode failed on match (NetworkErrorSendFailed), msgType: {msg.msgType}. matchServerID: {matchServerID}");
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
                    CoreLog.Critical.LogError($"SendToMatchNode failed on match (ClientErrorSendTimeout), msgType: {msg.msgType}. matchServerID: {matchServerID}");

                    //ERROR 전송 후 시간이 너무 많이 지났으므로 기존 등록했던 once dispathcer를 지우고 대기를 푼다.
                    m_MatchServiceDispatcher.UnregisterDispatcherOnce<T>(targetPacketUID);
                    taskOwner.Post(() =>
                    {
                        if (resultTask.IsCompleted is false)
                        {
                            resultTask.SetResult(new T()
                            {
                                errorCode = MsgErrorCode.ClientErrorSendTimeout
                            });
                        } // 뒤늦게 response가 올 경우 SetResult를 중복적으로 발생할 가능성이 있음.
                    });
                }, BaseConsts.ClientNodeSendTimeOut, false, timerName);

                OnTargetDispatchEventHandler<T> deleFunc = (msg, ctx) =>
                {
                    m_EventTimer.RemoveTimerEvent(timerID);

                    taskOwner.Post(() => 
                    {
                        var elaspedTick = TimeUtil.GetCurrentTickDiffMilliSec(prevTick);
                        CoreLog.Normal.LogInfo($"ResponseFromMatchNode, msgType: {msg.msgType}, elasped: {elaspedTick}");

                        if (resultTask.IsCompleted is false)
                        {
                            resultTask.SetResult(msg);
                        }
                    });
                };
                //원스 등록
                m_MatchServiceDispatcher.RegisterDispatcherOnce(targetPacketUID, deleFunc);
            });

            return resultTask;
        }

        internal CustomTask<T> SendToPickedMatchNode<T>(ITargetMsg msg, IActor taskOwner)
            where T : ITargetResMsg, new()
        {
            var matchServerID = m_MatchSessionManager.PickServerID();
            if (matchServerID == ConstEnum.Consts.INVALID_SERVER_ID)
            {
                var errorResult = new CustomTask<T>(string.Empty);
                errorResult.SetResult(new T()
                {
                    errorCode = MsgErrorCode.CoreErrorPickServerNoedFailed
                });
                return errorResult;
            }

            return SendToMatchNodeWithRes<T>(matchServerID, msg, taskOwner);
        }
    }
}
