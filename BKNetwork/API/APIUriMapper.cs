using Newtonsoft.Json;
using System.Text.RegularExpressions;
using BKServerBase.Logger;
using BKProtocol;
using BKProtocol.G2A;

namespace BKNetwork.API
{
    public static class APIUriMapper
    {
        public enum MethodType
        {
            GET = 0,
            POST = 3,
            PUT = 1,
            DELETE = 2
        };
        public delegate T GenerateDelegate<T>(string value)
        where T : IAPIResMsg, new();
        public static T GenerateResponseFromString<T>(string value)
        where T : IAPIResMsg, new()
        {
            try
            {
                if (value == string.Empty)
                {
                    return new T();
                }
                var result = JsonConvert.DeserializeObject<T>(value);
                if (result == null)
                {
                    CoreLog.Critical.LogWarning($"Json Deserialize Failed. message body: {value}, message Type : {new T().msgType}");
                    return new T();
                }
                return result;
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogError($"Json Deserialize Failed. message body: {value}, message Type :{new T().msgType}, EX: {e}");
                return new T();
            }
        }

        public abstract class IUriInfo
        {
            public string Path { get; private set; }
            public string Query { get; private set; }
            public bool SystemAPI { get; private set; }
            public MethodType Method { get; private set; }
            public Dictionary<string, string> headerInfo = new Dictionary<string, string>();
            public bool Retryable { get; private set; }
            public bool AllowOnBlockedChannel { get; private set; }
            public IUriInfo(string path, MethodType method, string query = "", bool systemAPI = false, bool allowOnBlockedChannel = false, bool retryable = false)
            {
                Path = path;
                Query = query;
                SystemAPI = systemAPI;
                Method = method;
                AllowOnBlockedChannel = allowOnBlockedChannel;
            }

            public HttpMethod GetHttpMethod()
            {
                switch (Method)
                {
                    case MethodType.GET:
                        return HttpMethod.Get;
                    case MethodType.POST:
                        return HttpMethod.Post;
                    case MethodType.PUT:
                        return HttpMethod.Put;
                    case MethodType.DELETE:
                        return HttpMethod.Delete;
                    default:
                        return HttpMethod.Get;
                }
            }

            public abstract IAPIResMsg GenResponse(string body);
        }

        public class UriInfo<T> : IUriInfo
            where T : IAPIResMsg, new() //응답메시지
        {
            public GenerateDelegate<T> GenFunc;
            public UriInfo(string path, MethodType method, string query = "", bool systemAPI = false, bool allowOnBlockedChannel = false, bool retryable = false)
            : base(path, method, query, systemAPI, allowOnBlockedChannel, retryable)
            {
                GenFunc = GenerateResponseFromString<T>;
            }

            public override IAPIResMsg GenResponse(string body)
            {
                return GenFunc(body);
            }
        }

        static readonly Dictionary<Type, IUriInfo> uriDictionary;
        public static readonly string HEADER_API_PROTOCOL_ERROR = "API-PROTOCOL-ERROR-CODE";
        //필요시 로그 계열을 헤더에 추가할 수 있음. 그외에도?
        public static readonly HttpRequestOptionsKey<bool> OPTION_KEY_API_RETRYABLE = new HttpRequestOptionsKey<bool>("S3-API-RETRYABLE");

        static APIUriMapper()
        {
            uriDictionary = new Dictionary<Type, IUriInfo>()
            {
                { typeof(APILoginReq), new UriInfo<APILoginRes>("/player/login",MethodType.PUT) },
                { typeof(APIChangeNameReq), new UriInfo<APIChangeNameRes>("/player/{playerUID}/changename",MethodType.PUT) },
                { typeof(APILogoutReq), new UriInfo<APILogoutRes>("/player/{playerUID}/logout",MethodType.PUT) },


                // TEAMS API
            };

            CheckInvalidUri();
        }

        public static bool IsAllowOnBlockedChannel(Type protocolType)
        {
            if (uriDictionary.TryGetValue(protocolType, out var uri) == false)
            {
                return false;
            }
            return uri.AllowOnBlockedChannel;
        }

        public static IUriInfo? GetUriInfo(Type protocolType)
        {
            return uriDictionary.TryGetValue(protocolType, out var uri) ? uri : null;
        }

        public static bool HasUriInfo(Type protocolType)
        {
            return uriDictionary.ContainsKey(protocolType);
        }

        public static string GetFormattedUri(string originalUri, IMsg msg)
        {
            string uri = originalUri;
            Regex regex = new Regex(@"{(?<param>[\w]*)}");
            MatchCollection mc = regex.Matches(originalUri);
            foreach (Match? m in mc)
            {
                string param = m!.Groups["param"].Value;
                var field = msg.GetType().GetProperty(param);
                string realValue = field?.GetValue(msg)?.ToString() ?? string.Empty;
                uri = uri.Replace(m.Value, realValue);
            }
            return uri;
        }

        public static void CheckInvalidUri()
        {
            foreach (var item in uriDictionary)
            {
                var msg = item.Key;
                Regex regex = new Regex(@"{(?<param>[\w]*)}");
                MatchCollection mcPath = regex.Matches(item.Value.Path);
                MatchCollection mcQuery = regex.Matches(item.Value.Query);
                foreach (Match? m in mcPath)
                {
                    string param = m!.Groups["param"].Value;
                    var field = msg.GetProperty(param);
                    if (field == null)
                    {
                        MiscLog.Critical.LogWarning($"can't find path parameter in Msg: {msg.Name}, param: {param})");
                    }
                }
                foreach (Match? m in mcQuery)
                {
                    string param = m!.Groups["param"].Value;
                    var field = msg.GetProperty(param);
                    if (field == null)
                    {
                        MiscLog.Critical.LogWarning($"can't find query parameter in Msg: {msg.Name}, param: {param})");
                    }
                }
            }
        }

        public static Dictionary<string, string>? GetFormattedHeader(IUriInfo uriInfo, IMsg msg, Type msgType)
        {
            if (uriInfo.headerInfo == null || uriInfo.headerInfo.Count == 0)
            {
                return null;
            }
            Dictionary<string, string> resultDic = new Dictionary<string, string>();
            foreach (var entry in uriInfo.headerInfo)
            {
                var field = msgType.GetProperty(entry.Value);
                string realValue = field?.GetValue(msg)?.ToString() ?? string.Empty;
                resultDic.Add(entry.Key, realValue);
            }
            return resultDic;

        }
    }
}

