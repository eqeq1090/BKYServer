using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.ConstEnum;
using BKServerBase.Interface;
using BKServerBase.Logger;
using BKServerBase.Messaging;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKCommonComponent.Detail;
using BKGameServerComponent.Actor;
using BKGameServerComponent.Network;
using BKGameServerComponent.Session;
using BKNetwork.API;
using BKProtocol;

namespace BKGameServerComponent.Channel
{
    internal sealed class CommonChannel : IRunnable, IJobActor<CommonChannel>
    {
        private readonly long m_ChannelID;
        private readonly Dictionary<PlayerUID, Player> m_PlayerMap = new Dictionary<PlayerUID, Player>();
        private readonly AtomicFlag m_Blocked = new AtomicFlag(false);
        private readonly AtomicFlag m_Disposed = new AtomicFlag(false);
        private readonly BackendDispatcher m_BackendDispatcher;
        private readonly JobDispatcher m_JobDispatcher;
        private int m_ObjectIDGenerator;
        private Dictionary<string/*teamID*/, List<PlayerUID>> m_TeamPlayerDict = new Dictionary<string, List<long>>();

        public CommonChannel(long channelID)
        {
            m_JobDispatcher = new JobDispatcher(true);
            m_ChannelID = channelID;
            m_BackendDispatcher = new BackendDispatcher(TaskSequencerMode.QueuePump, "CommonChannel_Backend", false, m_ChannelID);
        }

        public RunnableType RunnableType { get; } = RunnableType.Zone;
        public int ThreadWorkerID { get; private set; }
        public CommonChannel Owner => this;

        public void PendSend(IMsg msg, params PlayerUID[] playerIDs)
        {
            this.Post(self =>
            {
                foreach (var playerId in playerIDs)
                {
                    if (m_PlayerMap.ContainsKey(playerId) == false)
                    {
                        continue;
                    }

                    var player = m_PlayerMap[playerId];
                    player.SendToMe(msg);
                }
            });
        }

        public CustomTask<IPlayerActor?> PostGetPlayerAsync(PlayerUID playerID, IActor taskOwner)
        {
            return this.PostTask(self =>
            {
                if (m_PlayerMap.TryGetValue(playerID, out var player) is false)
                {
                    return null;
                }

                return (IPlayerActor)player;
            }, taskOwner);
        }

        public CustomTask<IPlayerActor?> PostAddPlayerAsync(SessionContext context, string backendSessionID, PlayerInfo playerInfo)
        {
            return this.PostTask(self =>
            {
                var player = new Player(
                        GetNewObjectID(),
                        context,
                        this,
                        m_BackendDispatcher,
                        backendSessionID,
                        playerInfo.playerUID);
                player.Initialize(playerInfo);

                if (m_PlayerMap.TryAdd(playerInfo.playerUID, player) == false)
                {
                    ContentsLog.Normal.LogError($"failed to register player, duplicated playerID: {playerInfo.playerUID} ");
                    return null;
                }

                var playerActor = player as IPlayerActor;
                return playerActor;
            }, context);
        }

        public void PostRemovePlayer(PlayerUID playerID, bool removeCache)
        {
            this.Post(async self =>
            {
                if (m_PlayerMap.Remove(playerID, out var player) == false)
                {
                    ContentsLog.Normal.LogError($"PostExitPlayer failed, player does not exist, playerID: {playerID}");
                    return;
                }

                if (removeCache == true)
                {
                    await player.OnRemoveAsync();
                }
                //TODO 이벤트 함수계열 등록
                //예시. 접속 상태 변경. 길드원 접속 이탈. 매칭 큐 취소 등.
                player.Dispose();
            });
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (m_Disposed.IsOn == true || disposing == false)
            {
                return;
            }
            if (m_Disposed.On() == false)
            {
                return;
            }

            this.Post(self =>
            {
                m_JobDispatcher.Dispose();
            });
        }

        public long GetID()
        {
            return m_ChannelID;
        }

        public int GetScore()
        {
            return ConstEnum.Consts.COMMON_CHANNEL_SCORE + (m_PlayerMap.Count * ConstEnum.Consts.SCORE_PER_PLAYER);
        }

        public void OnPostUpdate()
        {
            
        }

        public void OnUpdate()
        {
            m_JobDispatcher.RunAction();
            m_BackendDispatcher.ExecuteResponseHandlers();
        }

        public void PendAPIResMsg(long traceID, IAPIResMsg msg)
        {
            m_BackendDispatcher.EnqueueReseponse(traceID, msg);
        }

        public void SetBlocked(bool flag)
        {
            m_Blocked.On();
        }

        public void SetThread(Thread thread, int threadWorkerID)
        {
            ThreadWorkerID = threadWorkerID;
        }

        public JobDispatcher GetDispatcher()
        {
            return m_JobDispatcher;
        }
        
        private int GetNewObjectID()
        {
            return ++m_ObjectIDGenerator;
        }

        public int GetPlayerCount()
        {
            return m_PlayerMap.Count;
        }

        public bool ExistPlayer(long playerUID)
        {
            return m_PlayerMap.ContainsKey(playerUID);
        }

        public (long playerUID, string sessionID)[] GetSessionDatas()
        {
            return m_PlayerMap.Select(e => (e.Value.PlayerUID, e.Value.BackendSessionID)).ToArray();
        }

        public PresenceData[] GetPresenceDatas()
        {
            return m_PlayerMap.Select(e => e.Value.PresenceData).ToArray();
        }

        public bool TeamExist(string teamID)
        {
            return m_TeamPlayerDict.ContainsKey(teamID);
        }

        public void PubsubTeamMsg(string teamID, IPubsubMsg msg)
        {
            this.Post(self =>
            {
                //전용 디스패처 새로 만들자.
                //if (m_TeamPlayerDict.TryGetValue(teamID, out var list) == true)
                //{
                //    foreach (var playerUID in list)
                //    {
                //        if (m_PlayerMap.TryGetValue(playerUID, out var player) == true)
                //        {
                //            player.PostPubsubMsg(msg);
                //        }
                //    }
                //}
            });
        }
    }
}
