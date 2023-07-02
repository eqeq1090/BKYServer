using System.Collections.Concurrent;
using System.Threading.Channels;
using BKServerBase.Component;
using BKServerBase.ConstEnum;
using BKServerBase.Interface;
using BKServerBase.Messaging;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKCommonComponent.Detail;
using BKCommonComponent.Redis;
using BKGameServerComponent.Actor;
using BKGameServerComponent.Channel.Detail;
using BKGameServerComponent.Network;
using BKGameServerComponent.Session;
using BKNetwork.API;
using BKProtocol;

namespace BKGameServerComponent.Channel
{
    internal sealed class ChannelManager : IRunnable, IJobActor<ChannelManager>, IDisposable
    {
        private readonly long m_RunnableID;
        private readonly AtomicFlag m_Disposed = new AtomicFlag(false);
        private readonly Dictionary<long, CommonChannel> m_Channels = new Dictionary<long, CommonChannel>();
        private readonly Dictionary<long/*ChannelShard*/, (AtomicFlag running, ConcurrentQueue<APIRequestInfo> queue)> m_APIRequestQueueDict = new Dictionary<long, (AtomicFlag, ConcurrentQueue<APIRequestInfo>)>();
        private readonly JobDispatcher m_JobDispatcher;
        private long m_TraceIDGenerator;
        private ThreadCoordinator m_ThreadCoordinator;
        public RunnableType RunnableType { get; private set; } = RunnableType.System;
        public int ThreadWorkerID { get; private set; } = ConstEnum.Consts.CHANNEL_MANAGER_RUNNABLE_ID;
        public ChannelManager Owner => this;

        private APIDispatchComponent? m_ApiDispatcher;

        public ChannelManager(ThreadCoordinator threadCoordinator)
        {
            m_JobDispatcher = new JobDispatcher(true);
            m_ThreadCoordinator = threadCoordinator;
            m_RunnableID = threadCoordinator.MakeRunnableID();
        }

        private IRedisComponent RedisComponent = ComponentManager.Instance.GetComponent<IRedisComponent>()
            ?? throw new Exception($"RedisComponent is not registered in ComponentManager");

        public void Initialize()
        {
            for (int i = 0; i < ConstEnum.Consts.CHANNEL_MANAGER_APIQUEUE_SHARD_SIZE; ++i)
            {
                m_APIRequestQueueDict.Add(i, (new AtomicFlag(false), new ConcurrentQueue<APIRequestInfo>()));
            }
            for (int i = 0; i < ConstEnum.Consts.CHANNEL_LENGTH; ++i)
            {
                var newChannel = new CommonChannel(m_ThreadCoordinator.MakeRunnableID());
                m_ThreadCoordinator.AddRunnable(newChannel);
                m_Channels.Add(newChannel.GetID(), newChannel);
            }
            m_ThreadCoordinator.AddRunnable(this);
            m_ApiDispatcher = ComponentManager.Instance.GetComponent<APIDispatchComponent>() ?? 
                throw new Exception("APIDispatchComponent not initialized");
        }

        public long GetNewTraceID()
        {
            return Interlocked.Increment(ref m_TraceIDGenerator);
        }

        public CustomTask<IPlayerActor?> PostAddPlayerAsync(PlayerUID playerUID, SessionContext context, PlayerInfo playerInfo, string sessionKey)
        {
            var resultTask = new CustomTask<IPlayerActor?>(string.Empty);

            this.Post(async self =>
            {
                var channel = m_Channels.OrderBy(x => x.Value.GetPlayerCount()).FirstOrDefault();
                var player = await channel.Value.PostAddPlayerAsync(context, sessionKey, playerInfo);
                resultTask.SetResult(player);
            });

            return resultTask;
        }

        public void PostRemovePlayerAsync(long channelID, PlayerUID playerUID)
        {
            this.Post(self =>
            {
                if (m_Channels.TryGetValue(channelID, out var channel) == false)
                {
                    //ERROR
                    return;
                }
                channel.PostRemovePlayer(playerUID, true);
            });
        }

        public CustomTask PostTaskRemovePlayerAsync(long channelID, PlayerUID playerUID, IActor taskOwner)
        {
            return this.PostTask(self =>
            {
                if (m_Channels.TryGetValue(channelID, out var channel) == false)
                {
                    //ERROR
                    return;
                }
                channel.PostRemovePlayer(playerUID, false);
            }, taskOwner);
        }

        public CustomTask<IPlayerActor?> PostGetPlayerAsync(PlayerUID playerUID, IActor taskOwner)
        {
            var resultTask = new CustomTask<IPlayerActor?>(string.Empty);

            this.Post(async self =>
            {
                //2개 이상일 때 체크 필요
                var channel = m_Channels.Where(x => x.Value.ExistPlayer(playerUID)).Select(x=>x.Value).FirstOrDefault();
                if (channel != null)
                {
                    var player = await channel.PostGetPlayerAsync(playerUID, this);
                    taskOwner.GetDispatcher().Post(() =>
                    {
                        resultTask.SetResult(player);
                    });
                    return;
                }

                taskOwner.GetDispatcher().Post(() =>
                {
                    resultTask.SetResult(null);
                });
            });

            return resultTask;
        }

        public long PendAPIRequest<TRequest>(long channelID, long playerUID, TRequest msg, string sessionID)
            where TRequest : IMsg
        {
            var channelShardKey= channelID % ConstEnum.Consts.CHANNEL_MANAGER_APIQUEUE_SHARD_SIZE;
            if (m_APIRequestQueueDict.TryGetValue(channelShardKey, out var channelQueue) == false)
            {
                //ERROR
                return 0;
            }

            var msgType = typeof(TRequest);
            var newTraceID = GetNewTraceID();
            channelQueue.queue.Enqueue(new APIRequestInfo(newTraceID, channelID, playerUID, msg, sessionID, msgType));
            ExecuteQueue(channelShardKey);
            return newTraceID;
        }

        public void PendResMsgToChannel(long channelID, long traceID, IAPIResMsg msg)
        {
            if (m_Channels.TryGetValue(channelID, out var channel) == false)
            {
                return;
            }
            channel.PendAPIResMsg(traceID, msg);
        }

        public void PostUpdateRedisExpiry()
        {
            this.Post(self =>
            {
                foreach (var commonChannel in m_Channels.Values)
                {
                    var sessionDatas = commonChannel.GetSessionDatas();
                    if (sessionDatas.Length > 0)
                    {
                        RedisComponent.UpdateSessionDataExpiry(sessionDatas);
                    }

                    var presenceDatas = commonChannel.GetPresenceDatas();
                    if (presenceDatas.Length > 0)
                    {
                        RedisComponent.UpdatePresenceDataExpiry(presenceDatas);
                    }
                }
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public long GetID()
        {
            return m_RunnableID;
        }

        public int GetScore()
        {
            return 0;
        }

        public void OnPostUpdate()
        {

        }

        public void OnUpdate()
        {
            m_JobDispatcher.RunAction();
        }

        public void SetBlocked(bool flag)
        {
            
        }

        public void SetThread(Thread thread, int threadWorkerID)
        {
            ThreadWorkerID = threadWorkerID;
        }

        public JobDispatcher GetDispatcher()
        {
            return m_JobDispatcher;
        }

        private void ExecuteQueue(long shardKey)
        {
            if (m_APIRequestQueueDict.TryGetValue(shardKey, out var channelQueue) == false)
            {
                //ERROR
                return;
            }
            if (channelQueue.running.IsOn == true)
            {
                return;
            }
            if (channelQueue.queue.IsEmpty == true)
            {
                return;
            }
            if (channelQueue.running.On() == false)
            {
                return;
            }
            SendAPIRequest(shardKey);
        }

        private void SendAPIRequest(long shardKey)
        {
            if (m_APIRequestQueueDict.TryGetValue(shardKey, out var channelQueue) == false)
            {
                //ERROR
                return;
            }
            if (channelQueue.queue.TryPeek(out var request) == false)
            {
                return;
            }
            if (m_ApiDispatcher == null)
            {
                //ERROR
                return;
            }
            m_ApiDispatcher.APIRequest(request.PlayerUID, request.RequestMsg, request.TraceID, request.SessionID).ContinueWith((result) =>
            {
                //NOTE 일단 펌핑한다. 향후 응용단 레벨의 재전송 정책이 정해지면 바꾼다.
                channelQueue.queue.TryDequeue(out _);
                if (result.IsCompleted == false)
                {
                    //ERROR
                }
                PendResMsgToChannel(request.ChannelID, request.TraceID, result.Result);
                channelQueue.running.Off();
                if (channelQueue.queue.IsEmpty == false)
                {
                    ExecuteQueue(shardKey);
                }
            });
        }

        public void InvokeTeamPubsubMsgRouting(string teamID, IPubsubMsg msg)
        {
            var targetChannels = m_Channels.Where(x => x.Value.TeamExist(teamID));
            if (targetChannels.Count() == 0)
            {
                return;
            }
            foreach (var targetChannel in targetChannels)
            {
                targetChannel.Value.PubsubTeamMsg(teamID, msg);
            }
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

            foreach (var item in m_Channels.Keys)
            {
                if (m_Channels.Remove(item, out var channel) == true)
                {
                    channel.Dispose();
                    m_ThreadCoordinator.RemoveRunnable(channel);
                }
            }

            foreach (var item in m_APIRequestQueueDict.Keys)
            {
                if (m_APIRequestQueueDict.Remove(item, out var queue) == true)
                {
                    while (queue.queue.TryDequeue(out _)) ;
                }
            }

            m_JobDispatcher.Dispose();
        }
    }
}
