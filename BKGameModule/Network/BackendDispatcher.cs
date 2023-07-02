using Prometheus;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using BKServerBase.Component;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKGameServerComponent;
using BKNetwork.API;
using BKNetwork.Dispatch;
using BKProtocol;

namespace BKGameServerComponent.Network
{
    public class BackendDispatcher : IDisposable
    {
        private readonly TaskSequencerMode OwnerType;
        public class ResponseMessageHandlerPack
        {
            public ResponseMessageHandler Handler;
            public IMsg Req;
            public long StartTime;
            public ResponseMessageHandlerPack(ResponseMessageHandler handler, IMsg req)
            {
                Handler = handler;
                Req = req;
                StartTime = TimeUtil.GetCurrentTickMilliSec();
            }
            public bool Delayed()
            {
                if (StartTime + (ConfigManager.Instance.GameServerConf?.APIUserMaxWaitTime ?? 0) < TimeUtil.GetCurrentTickMilliSec())
                {
                    //로그를 남겨야 한다.
                    return true;
                }
                return false;
            }
        }

        private readonly string m_Name;
        private readonly APIDispatchComponent m_ApiDispatcher;
        private readonly bool m_ImmediateMode;
        private readonly long m_OwnerObjectID;

        private TaskSequencer m_apisTaskSequencer = new TaskSequencer();
        private CommandExecutor m_ResponseHandlerExecutor;
        private ConcurrentQueue<(long, IAPIResMsg)> m_ResponseMessageQueue = new ConcurrentQueue<(long, IAPIResMsg)>();
        private Dictionary<long, ResponseMessageHandlerPack> m_RequestedMessageMap = new Dictionary<long, ResponseMessageHandlerPack>();

        private long m_closed;
        private bool disposedValue;

        #region Metrics
        private static readonly Counter s_api_requests_total = Metrics.CreateCounter("request_total", "Backend Dispatcher", "kind");
        private static readonly Counter s_api_responses_total = Metrics.CreateCounter("response_total", "Backend Dispatcher", "kind");
        #endregion

        public BackendDispatcher(TaskSequencerMode ownerType, string name, bool IsImmediateMode, long objectID)
        {
            m_closed = 0;
            OwnerType = ownerType;
            ObjectCounter<BackendDispatcher>.Increment();
            m_Name = name;
            m_OwnerObjectID = objectID;
            m_ApiDispatcher = ComponentManager.Instance.GetComponent<APIDispatchComponent>()??throw new Exception("APIDispatchComponent not initialized");

            m_ImmediateMode = IsImmediateMode;
            m_ResponseHandlerExecutor = CommandExecutor.CreateCommandExecutor($"{m_Name}_Backend", objectID);
        }

        ~BackendDispatcher()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_ResponseHandlerExecutor.Dispose();
                    m_apisTaskSequencer.Destroy();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool IsEmpty()
        {
            return m_ResponseHandlerExecutor.IsEmpty() && m_apisTaskSequencer.IsEmpty() && m_RequestedMessageMap.Count == 0;
        }


        public (long, long) Length()
        {
            return (m_RequestedMessageMap.Count, m_ResponseHandlerExecutor.Length());
        }

        public void Close()
        {
            var closed = Interlocked.CompareExchange(ref m_closed, 1, 0);
            m_RequestedMessageMap.Clear();
            m_ResponseMessageQueue.Clear();
        }

        public void Destroy()
        {
            Close();
            m_apisTaskSequencer.Destroy();
        }

        public void EnqueueReseponse(long traceID, IAPIResMsg responseAns)
        {
            m_ResponseMessageQueue.Enqueue((traceID, responseAns));
        }

        public void ExecuteResponseHandlers()
        {
            while (m_ResponseMessageQueue.TryDequeue(out var result))
            {
                var traceID = result.Item1;
                var responseAns = result.Item2;
                if (m_RequestedMessageMap.TryGetValue(traceID, out var handler) == false)
                {
                    CoreLog.Critical.LogDebug($"backend request not found on handling response. traceID : (traceID), responseAns : fresponseAns.MsgName)");
                    continue;
                }
                handler.Handler(responseAns, true); //에러코드 체크하는 부분을 true로 강제치환해놨음
                m_RequestedMessageMap.Remove(traceID);
                s_api_responses_total.Inc();
            }
            m_ResponseHandlerExecutor.Execute();
        }


        public bool FindSlowQuery()
        {
            foreach (var requestItem in m_RequestedMessageMap.Values)
            {
                if (requestItem.Delayed() == true)
                {
                    return true;
                }
            }
            return false;
        }

        public bool TaskRequest(long channelID, long playerUID, IMsg msg, ResponseMessageHandler handler, string sessionID, long seq = 0, bool offline = false)
        {
            if (OwnerType == TaskSequencerMode.TickPump)
            {
                return TaskRequestV1(playerUID, msg, handler, sessionID, seq, offline);
            }
            else
            {
                return TaskRequestV2(channelID, playerUID, msg, handler, sessionID, seq, offline);
            }
        }

        public bool TaskRequestV1(long playerUID, IMsg msg, ResponseMessageHandler handler, string sessionID, long seq = 0, bool offline = false)
        {
            if (Interlocked.Read(ref m_closed) == 1)
            {
                CoreLog.Normal.LogWarning($"Requesting task on closed BackendDispatcher! " +
                    $"{msg} {ToString()} {new System.Diagnostics.StackTrace()}");
            }

            var traceID = 0; // 제너레이터로부터 받아오게 수정
            if (m_ImmediateMode == true)
            {
                s_api_requests_total.WithLabels(m_Name).Inc();
                m_ApiDispatcher.TaskRequest(
                    playerUID, 
                    sessionID, 
                    msg,
                    m_ResponseHandlerExecutor, 
                    (requestID, response) =>
                {
                    try
                    {
                        handler(requestID, response);
                    }
                    finally
                    {
                        s_api_responses_total.WithLabels(m_Name).Inc();
                    }
                }, traceID);
                return true;
            }
            else
            {
                var result = m_apisTaskSequencer.Enqueue((next) =>
                {
                    s_api_requests_total.WithLabels(m_Name).Inc();
                    m_ApiDispatcher.TaskRequest(playerUID, sessionID, msg, m_ResponseHandlerExecutor, (response, success) =>
                    {
                        try
                        {
                            try
                            {
                                handler(response, success);
                            }
                            finally
                            {
                                s_api_responses_total.WithLabels(m_Name).Inc();
                            }
                        }
                        finally
                        {
                            next();
                        }
                    }, traceID);
                });
                return result;
            }
        }

        public bool TaskRequestV2(long channelID, long playerUID, IMsg msg, ResponseMessageHandler handler, string sessionID, long seq = 0, bool offline = false)
        {
            if (Interlocked.Read(ref m_closed) == 1)
            {
                CoreLog.Normal.LogWarning($"Requesting task on closed BackendDispatcher! {msg}");
                return false;
            }
            s_api_requests_total.WithLabels(m_Name).Inc();
            //이게 핵심. 백엔드 큐 매니저 짜두자
            var traceID = GameServerComponent.Instance.EnqueueBackEndRequest(channelID, msg, playerUID, sessionID);
            m_RequestedMessageMap.Add(traceID, new ResponseMessageHandlerPack(handler, msg));
            return true;
        }

        public CustomTask<TResponse> RequestAsync<TResponse>(IMsg msg, long playerUID, string sessionID, long channelID = 0, long seq = 0, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            where TResponse : IAPIResMsg, new()
        {
            var task = new CustomTask<TResponse>(TagBuilder.MakeTag(file, line));
            var result = TaskRequest(channelID, playerUID, msg, (res, _) =>
            {
                if (res is not TResponse responseAns)
                {
                    CoreLog.Critical.LogWarning($"Response Message Type Not Matched. expected : (typeof(T)), actual : fres.MsgName)");
                    return;
                }

                task.SetResult(responseAns);
                {
                    if (responseAns.errorCode is (MsgErrorCode.ApiErrorSessionExpired or MsgErrorCode.ApiErrorInvalidSessionID))
                    {

                    } // TODO: 해당 에러는 킥 필요.
                    //에러코드 체크
                    /*
                    if (res.errorCode != eProtocolError.ERROR_NONE)
                    {
                        return;
                    }*/
                }
            }, sessionID, seq);
            if (result == false)
            {
                var errorAns = new TResponse()
                {
                    //에러코드 세팅
                    //errorCode = blah
                };
                task.SetResult(errorAns);
            }
            return task;
        }

        public void SetThread(Thread thread)
        {
            m_apisTaskSequencer.SetThread(thread);
        }
    }
}
