using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKGameServerComponent.Actor;
using BKGameServerComponent.Channel;
using BKGameServerComponent.Session;
using BKNetwork.ConstEnum;
using BKNetwork.Interface;
using BKProtocol;
using BKProtocol.Pubsub;

namespace BKGameServerComponent
{
    public partial class GameServerComponent
    {
        internal long EnqueueBackEndRequest<TRequest>(long channelID, TRequest msg, long playerUID, string sessionID) //seq, offline
            where TRequest : IMsg
        {
            return m_ChannelManager.PendAPIRequest(channelID, playerUID, msg, sessionID);
        }

        internal void PostRemovePlayerAsync(long channelID, PlayerUID playerUID)
        {
            m_ChannelManager.PostRemovePlayerAsync(channelID, playerUID);
            m_PlayerSessionDict.TryRemove(playerUID, out _);
        }

        internal async CustomTask PostTaskRemovePlayerAsync(long channelID, PlayerUID playerUID)
        {
            await m_ChannelManager.PostTaskRemovePlayerAsync(channelID, playerUID, this);
            m_PlayerSessionDict.TryRemove(playerUID, out _);
        }

        internal async CustomTask<(MsgErrorCode, IPlayerActor?)> PostAddPlayerAsync(PlayerUID playerUID, SessionContext context, PlayerInfo info, string sessionKey)
        {
            if (m_PlayerSessionDict.TryGetValue(playerUID, out var sessionID) == true)
            {
                //기존 플레이어를 찾아서 모든 매니저에서 해제시킨다.

                var player = await m_ChannelManager.PostGetPlayerAsync(playerUID, this);
                if (player != null)
                {
                    await PostTaskRemovePlayerAsync(player.ChannelID, playerUID);
                }
                await m_ClientSessionManager.PostTaskCloseSession(sessionID, DisconnectReason.DuplicateConnect, this);
                m_PlayerSessionDict.TryRemove(playerUID, out _);
                //TODO sessionKey를 포함한 pubsub을 보내서 다른 서버에서 다른 sessionkey를 가지고 있는 playeruid 기반 객체를 제거할 수 있게 유도한다.
            }
            var result = await InvokePubsubMessage(BKRedisDataType.session, new SyncLoginPlayerMsg()
            {
                SenderNodeID = ConfigManager.Instance.CommonConfig.ServerNodeId,
                PlayerUIDs = new List<long> { playerUID },
            }, this);
            if (result is false)
            {
                ContentsLog.Critical.LogError($"publish SyncLoginPlayerMsg failed, playerUID: {playerUID}");
            }

            var newPlayer = await m_ChannelManager.PostAddPlayerAsync(playerUID, context, info, sessionKey);
            if (newPlayer == null)
            {
                return (MsgErrorCode.InvalidErrorCode, null);
            }
            m_PlayerSessionDict.TryAdd(playerUID, context.GetSessionID());
            return (MsgErrorCode.Success, newPlayer);
        }

        //NOTE() 주의사항 : 아랫걸로 player 받아갔더라도 거기에 메모리 직접 조작하지 마세요.
        internal async CustomTask<IPlayerActor?> PostGetPlayerAsync(PlayerUID playerUID, IActor taskOwner)
        {
            return await m_ChannelManager.PostGetPlayerAsync(playerUID, taskOwner);
        }
    }
}
