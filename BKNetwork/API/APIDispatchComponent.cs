using EmbedIO.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using Prometheus;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BKServerBase.Component;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKNetwork.ConstEnum;
using BKNetwork.Discovery;
using BKNetwork.Dispatch;
using BKProtocol;
using BKProtocol.Enum;
using static BKNetwork.API.LoadBalancer;

namespace BKNetwork.API
{
    public class APIDispatchComponent : BaseSingleton<APIDispatchComponent>, IComponent
    {
        private static AsyncLocal<long> s_timestamp = new AsyncLocal<long>();
        private static readonly Histogram s_api_response_duration = Metrics
            .CreateHistogram("s3_game_api_responses_duration", "API Response Duration (ms)",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(start: 5.0, factor: 2.0, count: 12)
            });
        private static readonly Counter s_api_reponses_status_code = Metrics
            .CreateCounter("api_responses_status_code", "API Response Status Code", "kind");

        private readonly IHttpClientFactory? m_httpClientFactory;
        public ServiceDiscoveryManager? ServiceDiscovery { get; private set; }
        private ICommandExecutor m_CommandExecutor;

        public APIDispatchComponent()
        {
            var services = new ServiceCollection();
            services.AddHttpClient("API", c =>
            {
                c.Timeout = TimeSpan.FromMinutes(Consts.TIMEOUT_BACKEND_API_MINUTES);
                c.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                c.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
                c.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            })
            .UseHttpClientMetrics()
            .AddPolicyHandler(CreateRetryPolicyHandler())
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
            {
                //TODO 압축 도입이 필요한 시점에 처리
                //AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                MaxConnectionsPerServer = ConfigManager.Instance.GameServerConf?.ApiMaxConnectionPerServer?? Consts.DEFAULT_MAX_API_CONNECTION
            });

            var serviceProvider = services.BuildServiceProvider();
            m_httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

            m_CommandExecutor = CommandExecutor.CreateCommandExecutor("APIDispatchComponent", 0);
            //NOTE API 서버의 로드밸런싱 정보를 쳐다보고 싶으므로 APIServer를 지정
        }

        public void SetServiceDiscovery(ServiceDiscoveryManager serviceDiscoveryManager)
        {
            if (ServiceDiscovery == null)
            {
                ServiceDiscovery = serviceDiscoveryManager;
                ServiceDiscovery.SetCommandExecutor(m_CommandExecutor);
            }
        }

        public (bool success, OnComponentInitializedHandler? InitDoneFunc) Initialize()
        {
            if (ServiceDiscovery == null)
            {
                return (false, null);
            }
            ServiceDiscovery.Initialize();
            return (true, null);
        }

        public void Invoke(Command command)
        {
            m_CommandExecutor.Invoke(command);
        }

        public bool OnUpdate(double delta)
        {
            m_CommandExecutor.Execute();
            return true;
        }

        public bool Shutdown()
        {
            return true;
        }

        private static void Log(IAPIResMsg response, string responseString, HttpMethod method, Uri uri, HttpStatusCode statusCode, long duration, string recvTraceID)
        {
            if (ConfigManager.Instance.CommonConfig.NetworkLogFlag == false)
            {
                return;
            }
            //하드코딩
            if (response.errorCode ==  MsgErrorCode.Success)
            {
                var truncateResponse = CommonUtil.TruncateJsonString(responseString, 4096);
                if (ConfigManager.Instance.GameServerConf?.ShowApiBodyLog ?? false == true)
                {
                    GameNetworkLog.Normal.LogDebug($"<== Response : {statusCode} {method} {uri} {truncateResponse} {duration} {recvTraceID}");
                }
                else
                {
                    GameNetworkLog.Normal.LogDebug($"<== Response : {statusCode} {method} {uri} {duration} {recvTraceID}");
                }
            }
            else
            {
                if (statusCode == HttpStatusCode.OK)
                {
                    GameNetworkLog.Normal.LogDebug($"<== Response : {statusCode} {method} {uri} {response.errorCode} {duration} {recvTraceID}");
                }

                else
                {
                    if ((int)statusCode >= 400 && (int)statusCode < 500)
                    {
                        GameNetworkLog.Critical.LogWarning($"<== Response : {statusCode} {method} {uri} {response.errorCode} {duration} {recvTraceID}");
                    }
                    else
                    {
                        GameNetworkLog.Critical.LogError($"<== Response : {statusCode} {method} {uri} {response.errorCode} {duration} {recvTraceID}");
                    }
                }
            }
        }
        private Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> CreateRetryPolicyHandler()
        {
            var retryPolicy = HttpPolicyExtensions.HandleTransientHttpError()
            .OrInner<IOException>()
            .OrInner<SocketException>()
            .OrResult(response => ConfigManager.Instance.GameServerConf?.LBRetryStatusCodes.Contains(response.StatusCode) ?? false)
            .WaitAndRetryAsync(
            retryCount: ConfigManager.Instance.GameServerConf?.LBRetryMaxCount ?? 1, //TODO const로 빼야하나?
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * ConfigManager.Instance.GameServerConf?.LBRetryFirstBackoffDelay ?? 1000), //TODO const로 빼야하나?
            onRetry: (Action<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context>)((result, retryDelay, retryCount, context) =>
            {
                LogExtensions.LogWarning(
                GameNetworkLog.Critical, "HttpRequest failed. onRetry " +
                $"statusCode: {result.Result?.StatusCode.ToString() ?? string.Empty} " +
                $"method: {result.Result?.RequestMessage?.Method.ToString() ?? string.Empty} " +
                $"uri: {result.Result?.RequestMessage?.RequestUri?.ToString() ?? string.Empty} " +
                $"retryDelay: {retryDelay.TotalMilliseconds} " +
                $"retrycount: {retryCount}" +
                    $"exception: {result.Exception?.ToString() ?? string.Empty}");
            }));
            var noOpAsync = Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
            return (request) =>
            {
                if (ConfigManager.Instance.GameServerConf?.UseApiLB ?? false == false)
                {
                    return noOpAsync;
                }

                if (ConfigManager.Instance.GameServerConf?.LBRetryMethods.Contains(request.Method.ToString()) ?? false == false)
                {
                    return noOpAsync;
                }

                if (request.Options.TryGetValue(APIUriMapper.OPTION_KEY_API_RETRYABLE, out var retryable) == false)
                {
                    return noOpAsync;
                }
                if (retryable == false)
                {
                    return noOpAsync;
                }
                return retryPolicy;
            };
        }

        private void Dispatch(
            IAPIResMsg responseAns,
            string responseJson,
            ICommandExecutor? responseHandlerExecutor,
            ResponseMessageHandler responseHandler,
            HttpRequestMessage request,
            Uri uri,
            HttpStatusCode statusCode,
            long duration,
            string recvTraceID,
            int headerErrorCode = 0)
            {
                if (responseHandlerExecutor == null)
                {
                    try
                    {
                        Log(responseAns, responseJson, request.Method, uri, statusCode, duration, recvTraceID);
                    }
                    finally
                    {
                        responseHandler(responseAns, success: true);
                    }
                    return;
                }
                var result = responseHandlerExecutor.Invoke(() =>
                {
                    try
                    {
                        Log(responseAns, responseJson, request.Method, uri, statusCode, duration, recvTraceID);
                    }
                    finally
                    {
                        responseHandler(responseAns, success: true);
                    }
                });
                if (!result)
                {
                    GameNetworkLog.Critical.LogWarning($"Can't Invoke Handler on Dispatch!");
                }
            }

        public static void DispatchError(
            IMsg msg,
            ICommandExecutor? responseHandlerExecutor,
            ResponseMessageHandler responseHandler,
            MsgErrorCode protocolError)
        {
            var ans = new IAPIResMsg(MsgType.Invalid)
            {
                errorCode = MsgErrorCode.ApiErrorExceptionOccurred
            };
            var uriInfo = APIUriMapper.GetUriInfo(msg.GetType());
            if (uriInfo == null)
            {

            }
            else
            {
                ans = uriInfo.GenResponse(string.Empty);
                ans.errorCode = protocolError;
                //에러코드 맵핑
            }

            if (responseHandlerExecutor == null)
            {
                responseHandler(ans, success: false);
            }
            else
            {
                var result = responseHandlerExecutor.Invoke(() => responseHandler(ans, success: false));
                if (!result)
                {
                    GameNetworkLog.Critical.LogWarning($"Can't Invoke Handler on DispatchError!");
                }
            }
        }

        public async void TaskRequest(
            long playerUID,
            string sessionID,
            IMsg msg,
            ICommandExecutor? responseHandlerExecutor,
            ResponseMessageHandler responseHandler,
            long traceID)
        {
            var msgType = msg.GetType();
            var uriInfo = APIUriMapper.GetUriInfo(msgType);
            if (uriInfo == null)
            {
                DispatchError(msg, responseHandlerExecutor, responseHandler, 0);
                return;
            }
            UriBuilder? uriBuilder = null;
            long duration = 0;
            try
            {
                if (ServiceDiscovery == null)
                {
                    DispatchError(msg, responseHandlerExecutor, responseHandler, 0);
                    return;
                }
                var hostPair = ServiceDiscovery.ResolveAPIS();

                uriBuilder = new UriBuilder()
                {
                    Host = hostPair.Host,
                    Port = hostPair.Port
                };
                if (uriInfo.Path != null && uriInfo.Path.Length > 0)
                {
                    uriBuilder.Path = APIUriMapper.GetFormattedUri(uriInfo.Path, msg);
                }
                if (uriInfo.Query != null && uriInfo.Query.Length > 0)
                {
                    uriBuilder.Query = APIUriMapper.GetFormattedUri(uriInfo.Query, msg);
                }
                HttpMethod httpMethod = uriInfo.GetHttpMethod();
                using var request = new HttpRequestMessage(httpMethod, uriBuilder.Uri);
                request.Headers.Add(Consts.MESSAGE_TYPE, $"{msg.msgType}");
                request.Headers.Add(Consts.PLAYER_UID_HTTP_HEADER_STRINGKEY, playerUID.ToString());
                request.Headers.Add(Consts.SESSION_ID_HTTP_HEADER_STRINGKEY, sessionID);

                var headerInfo = APIUriMapper.GetFormattedHeader(uriInfo, msg, msgType);
                if (headerInfo != null && headerInfo.Count > 0)
                {
                    foreach (var entry in headerInfo)
                    {
                        request.Headers.Add(entry.Key, entry.Value);
                    }
                }

                request.Headers.Add(Consts.HEADER_TRACE_ID, traceID.ToString());
                request.Options.Set(APIUriMapper.OPTION_KEY_API_RETRYABLE, uriInfo.Retryable);
                // body
                if (uriInfo.Method == APIUriMapper.MethodType.PUT || uriInfo.Method == APIUriMapper.MethodType.POST)
                {
                    string requestJson = JsonConvert.SerializeObject(msg);//, new Newtonsoft.Json.Converters.StringEnumConverter());
                    request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    if (ConfigManager.Instance.CommonConfig.NetworkLogFlag == true)
                    {
                        if (ConfigManager.Instance.GameServerConf?.ShowApiBodyLog ?? false == true)
                        {
                            GameNetworkLog.Normal.LogDebug($"==> {uriInfo.Method} {uriBuilder.Uri} {msg} {requestJson} {traceID}");
                        }
                        else
                        {
                            GameNetworkLog.Normal.LogDebug($"==> {uriInfo.Method} {uriBuilder.Uri} {msg} {traceID}");
                        }
                    }
                }
                else
                {
                    if (ConfigManager.Instance.CommonConfig.NetworkLogFlag == true)
                    {
                        GameNetworkLog.Normal.LogDebug($"===> {uriInfo.Method} {uriBuilder.Uri} {msg} {traceID}");
                    }
                }
                var client = m_httpClientFactory!.CreateClient("API");

                s_timestamp.Value = TimeUtil.GetCurrentTickMilliSec();
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                {
                    duration = TimeUtil.GetCurrentTickDiffMilliSec(s_timestamp.Value);
                    s_api_response_duration.Observe(duration);
                    var statusCode = (int)response.StatusCode;
                    s_api_reponses_status_code.WithLabels(statusCode.ToString()).Inc();

                    if (statusCode >= 500 && statusCode < 600)
                    {
                        // 5XX ServerError
                        GameNetworkLog.Critical.LogError($"«== {uriInfo.Method} {uriBuilder.Uri} {msg} {response.StatusCode} {duration} {traceID}");

                        throw new ResponseErrorException(5, $"{response.StatusCode}");
                    }
                    else
                    {
                        var headerErrorCode = 0;
                        if (response.Headers.TryGetValues(APIUriMapper.HEADER_API_PROTOCOL_ERROR, out var errorCodes))
                        {
                            using var enumerator = errorCodes.GetEnumerator();
                            if (enumerator.MoveNext())
                            {
                                if (int.TryParse(enumerator.Current, out var errorCodeValue))
                                {
                                    headerErrorCode = errorCodeValue;
                                }
                            }
                        }
                        await using var stream = await response.Content.ReadAsStreamAsync();
                        string recvTraceID = string.Empty;
                        if (stream.CanRead == false)
                        {
                            GameNetworkLog.Critical.LogWarning("Can Read Packet");
                            DispatchError(msg, responseHandlerExecutor, responseHandler, MsgErrorCode.ApiErrorCantReadResponse);
                            return;
                        }
                        if (response.Headers.TryGetValues(Consts.HEADER_TRACE_ID, out IEnumerable<string>? headerValues))
                        {
                            var enumerator = headerValues.GetEnumerator();
                            if (enumerator.MoveNext())
                            {
                                recvTraceID = enumerator.Current;
                            }
                        }

                        using var sr = new StreamReader(stream);
                        var responseString = await sr.ReadToEndAsync();
                        var responseAns = uriInfo.GenResponse(responseString);
                        if (headerErrorCode != 0)
                        {
                            //   responseAns.errorCode = headerErrorCode;
                        }
                        if ((statusCode >= 200 && statusCode < 300) == false)// && responseAns.errorCode == 0)
                        {
                            //responseAns.errorCode = 에러코드
                        }
                        Dispatch(responseAns, responseString, responseHandlerExecutor, responseHandler, request, uriBuilder.Uri, response.StatusCode, duration, recvTraceID, headerErrorCode);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                GameNetworkLog.Critical.LogError($"APIDispatchComponentException.HttpRequestException: {e}");
                DispatchError(msg, responseHandlerExecutor, responseHandler, MsgErrorCode.ApiErrorExceptionOccurred);
            }
            catch (ResponseErrorException e)
            {
                GameNetworkLog.Critical.LogError($"APIDispatchComponentException.ResponseErrorException: {e}");
                DispatchError(msg, responseHandlerExecutor, responseHandler, MsgErrorCode.ApiErrorExceptionOccurred);
            }
            catch (TaskCanceledException e)
            {
                GameNetworkLog.Critical.LogError($"APIDispatchComponentException.TaskCanceledException: {e}");
                DispatchError(msg, responseHandlerExecutor, responseHandler, MsgErrorCode.ApiErrorExceptionOccurred);
            }
            catch (Exception e)
            {
                GameNetworkLog.Critical.LogError($"APIDispatchComponentException: {e}");
                DispatchError(msg, responseHandlerExecutor, responseHandler, MsgErrorCode.ApiErrorExceptionOccurred);
            }
        }

        //TODO 중복 계정 접속의 eventually consistency 확보를 위해 seq 정보 필수 할당으로 변경 필요
        public async Task<IAPIResMsg> APIRequest(long playerUID, IMsg msg, long traceID, string sessionID, long seq = 0)
        {
            var msgType = msg.GetType();
            var uriInfo = APIUriMapper.GetUriInfo(msgType);
            if (uriInfo == null)
            {
                return APIError(msg, MsgErrorCode.ApiErrorUrlNotFound);
            }
            UriBuilder? uriBuilder = null;
            
            try
            {
                if (ServiceDiscovery == null)
                {
                    return APIError(msg, MsgErrorCode.InvalidErrorCode);
                }
                var hostPair = ServiceDiscovery.ResolveAPIS();
                uriBuilder = new UriBuilder()
                {
                    Host = hostPair.Host,
                    Port = hostPair.Port
                };
                if (uriInfo.Path != null && uriInfo.Path.Length > 0)
                {
                    uriBuilder.Path = APIUriMapper.GetFormattedUri(uriInfo.Path, msg);
                }
                if (uriInfo.Query != null && uriInfo.Query.Length > 0)
                {
                    uriBuilder.Query = APIUriMapper.GetFormattedUri(uriInfo.Query, msg);
                }

                HttpMethod httpMethod = uriInfo.GetHttpMethod();
                using var request = new HttpRequestMessage(httpMethod, uriBuilder.Uri);
                request.Headers.Add(Consts.MESSAGE_TYPE, $"{msg.msgType}");
                if (playerUID == 0)
                {
                    if (!uriInfo.SystemAPI)
                    {
                        GameNetworkLog.Critical.LogDebug($"Requesting without User ID! (uriBuilder.Uri)");
                    }
                }
                else
                {
                    request.Headers.Add(Consts.PLAYER_UID_HTTP_HEADER_STRINGKEY, playerUID.ToString());
                    request.Headers.Add(Consts.SESSION_ID_HTTP_HEADER_STRINGKEY, sessionID.ToString());
                }
                var headerInfo = APIUriMapper.GetFormattedHeader(uriInfo, msg, msgType);
                if (headerInfo != null && headerInfo.Count > 0)
                {
                    foreach (var entry in headerInfo)
                    {
                        request.Headers.Add(entry.Key, entry.Value);
                    }
                }

                request.Headers.Add(Consts.HEADER_TRACE_ID, traceID.ToString());
                request.Options.Set(APIUriMapper.OPTION_KEY_API_RETRYABLE, uriInfo.Retryable);

                if (uriInfo.Method == APIUriMapper.MethodType.PUT || uriInfo.Method == APIUriMapper.MethodType.POST)
                {
                    string requestJson = JsonConvert.SerializeObject(msg); // new Newtonsoft.Json.Converters.StringEnumConverter());
                    request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    if (ConfigManager.Instance.CommonConfig.NetworkLogFlag == true)
                    {
                        if (ConfigManager.Instance.GameServerConf?.ShowApiBodyLog ?? false == true)
                        {
                            GameNetworkLog.Normal.LogDebug($"=> {uriInfo.Method} {uriBuilder.Uri} {msg} {requestJson} {traceID}");
                        }
                        else
                        {
                            GameNetworkLog.Normal.LogDebug($"=> {uriInfo.Method} {uriBuilder.Uri} {msg} {traceID}");
                        }
                    }
                }
                else
                {
                    if (ConfigManager.Instance.CommonConfig.NetworkLogFlag == true)
                    {
                        GameNetworkLog.Normal.LogDebug($"==> {uriInfo.Method} {uriBuilder.Uri} {msg} {traceID}");
                    }
                }
                var client = m_httpClientFactory!.CreateClient("API");
                s_timestamp.Value = TimeUtil.GetCurrentTickMilliSec();
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                long duration = TimeUtil.GetCurrentTickDiffMilliSec(s_timestamp.Value);
                s_api_response_duration.Observe(duration);
                var statusCode = (int)response.StatusCode;
                s_api_reponses_status_code.WithLabels(statusCode.ToString()).Inc();
                if (statusCode >= 500 && statusCode < 600)
                {
                    GameNetworkLog.Critical.LogError($"«== {uriInfo.Method} {uriBuilder.Uri} {msg} {response.StatusCode} {duration} {traceID}");
                    throw new ResponseErrorException(3, $"{response.StatusCode}");
                }
                else if (statusCode == 404)
                {
                    GameNetworkLog.Critical.LogError($"«== {uriInfo.Method} {uriBuilder.Uri} {msg} {response.StatusCode} {duration} {traceID}");
                    throw new ResponseErrorException(3, $"{response.StatusCode}");
                }
                else
                {
                    var headerErrorCode = 0;
                    if (response.Headers.TryGetValues(APIUriMapper.HEADER_API_PROTOCOL_ERROR, out var errorCodes))
                    {
                        using var enumerator = errorCodes.GetEnumerator();
                        if (enumerator.MoveNext())
                        {
                            if (int.TryParse(enumerator.Current, out var errorCodeValue))
                            {
                                headerErrorCode = errorCodeValue;
                            }
                        }
                    }

                    await using var stream = await response.Content.ReadAsStreamAsync();
                    string recvTraceID = string.Empty;
                    if (stream.CanRead == false)
                    {
                        GameNetworkLog.Critical.LogWarning("Can Read Packet");
                        return APIError(msg, MsgErrorCode.ApiErrorCantReadResponse);
                    }

                    if (response.Headers.TryGetValues(Consts.HEADER_TRACE_ID, out IEnumerable<string>? headerValues))
                    {
                        var enumerator = headerValues.GetEnumerator();
                        if (enumerator.MoveNext())
                        {
                            recvTraceID = enumerator.Current;
                        }
                    }

                    using var sr = new StreamReader(stream);
                    var responseString = await sr.ReadToEndAsync();
                    var responseAns = uriInfo.GenResponse(responseString);
                    if (headerErrorCode != 0)
                    {
                        //responseAns.errorCode = headerErrorCode;
                    }

                    if ((statusCode >= 200 && statusCode < 300) == false)
                    {
                        //responseAns.errorCode = eProtocolError.API_RESPONSE_ERROR_SERVER;
                    }
                    Log(responseAns, responseString, request.Method, uriBuilder.Uri, response.StatusCode, duration, recvTraceID);
                    return responseAns;
                }
            }
            catch (HttpRequestException e)
            {
                GameNetworkLog.Critical.LogError($"APIDispatchComponentException {uriInfo?.Method} {uriBuilder?.Uri} {msg} {traceID} {e}");
                return APIError(msg, MsgErrorCode.ApiErrorExceptionOccurred);
            }
            catch (ResponseErrorException e)
            {
                GameNetworkLog.Critical.LogError($"APIDispatchComponentException {uriInfo?.Method} {uriBuilder?.Uri} {msg} {traceID} {e}");
                return APIError(msg, MsgErrorCode.ApiErrorExceptionOccurred);
            }

            catch (TaskCanceledException e)
            {
                GameNetworkLog.Critical.LogError($"APIDispatchComponentException furiInfo?. Method) {uriBuilder?.Uri} {msg} {traceID} {e}");
                return APIError(msg, MsgErrorCode.ApiErrorExceptionOccurred);
            }
            catch (Exception e)
            {
                GameNetworkLog.Critical.LogError($"APIDispatchComponentException (uriInfo?.Method) {uriBuilder?.Uri} {msg} {traceID} {e}");
                return APIError(msg, MsgErrorCode.ApiErrorExceptionOccurred);
            }
        }

        public IAPIResMsg APIError(IMsg msg, MsgErrorCode protocolError)
        {
            var ans = new IAPIResMsg(MsgType.Invalid);
            var uriInfo = APIUriMapper.GetUriInfo(msg.GetType());
            if (uriInfo == null)
            {
                ans.errorCode = MsgErrorCode.ApiErrorUrlNotFound;
            }
            else
            {
                ans = uriInfo.GenResponse(string.Empty);
                ans.errorCode = protocolError;
            }
            return ans;
        }
    }
}
