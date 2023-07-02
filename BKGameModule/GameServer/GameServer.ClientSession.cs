using Amazon.S3.Model;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKCommonComponent.Detail;
using BKCommonComponent.Redis.Detail;
using BKGameServerComponent.Actor.Detail;
using BKGameServerComponent.Network;
using BKGameServerComponent.Session;
using BKNetwork.ConstEnum;
using BKNetwork.Interface;
using BKProtocol;
using BKProtocol.Pubsub;

namespace BKGameServerComponent
{
    public partial class GameServerComponent
    {
        public void OnClientSessionAdd(ISession session)
        {
            var sessionContext = new SessionContext(m_ClientSessionManager, session, m_ClientSessionManager.GetBackendDispatcher());
            m_ClientSessionManager.Add(sessionContext);
        }

        public void OnClientSessionRemove(IContext context, DisconnectReason reason)
        {
            m_ClientSessionManager.Remove(context.GetSessionID(), reason);
        }

        public void OnClientSessionError(IContext context, Exception ex)
        {
            //ex 출력
            m_ClientSessionManager.Remove(context.GetSessionID(), DisconnectReason.SessionError);
        }

        public void InvokeTargetPubsubMsgRouting(long playerUID, IPubsubMsg msg)
        {
            if (m_PlayerSessionDict.TryGetValue(playerUID, out var sessionID))
            {
                m_ClientSessionManager.InvokeTargetPubsubMsgRouting(sessionID, msg);
            }
        }

        public void InvokeTeamPubsubMsgRouting(string teamID, IPubsubMsg msg)
        {
            m_ChannelManager.InvokeTeamPubsubMsgRouting(teamID, msg);
        }
   
        public void CloseUserSession(SessionID sessionID, DisconnectReason reason)
        {
            m_ClientSessionManager.PostCloseSession(sessionID, reason);
        }

        public void CheckDuplicatePlayer(List<PlayerUID> playerUIDs)
        {
            var sessionIDList = m_PlayerSessionDict.Where(x => playerUIDs.Contains(x.Key) == true).Select(x=>x.Value).ToList();
            if (sessionIDList.Count == 0)
            {
                return;
            }
            foreach (var sessionID in sessionIDList)
            {
                CloseUserSession(sessionID, DisconnectReason.DuplicateConnect);
            }
        }
    }
}
