using Amazon.Runtime.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKProtocol;
using BKProtocol.Enum;
using BKProtocol.G2A;
using BKWebAPIComponent.ConstEnum;
using BKWebAPIComponent.Redis;

namespace BKWebAPIComponent.Middleware
{
    public class SessionCheckMiddleware
    {
        private readonly RequestDelegate m_Next;
        private readonly HashSet<MsgType> m_PassingMsg = new HashSet<MsgType>()
        {
            MsgType.GtoA_LoginReq,
            MsgType.MtoA_APIGameRoomEndReq,
            MsgType.MtoA_APIPlayerGameInfoReq,
            MsgType.MtoA_GetRoomSlotInfoReq,
        };

        public SessionCheckMiddleware(RequestDelegate next)
        {
            m_Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            MsgErrorCode errorCode = MsgErrorCode.Success;
            //레디스 서비스
            try
            {
                var request = context.Request;
                if (context.Request.Headers.TryGetValue(BKNetwork.ConstEnum.Consts.MESSAGE_TYPE, out var msgTypeStr) == false)
                {
                    //ERROR
                    errorCode = MsgErrorCode.InvalidErrorCode;
                    return;
                }
                if (Enum.TryParse<MsgType>(msgTypeStr, out var msgTypeValue) == false)
                {
                    errorCode = MsgErrorCode.InvalidErrorCode;
                    return;
                }
                if (m_PassingMsg.Contains(msgTypeValue))
                {
                    return;
                }

                if (context.Request.Headers.TryGetValue(BKNetwork.ConstEnum.Consts.PLAYER_UID_HTTP_HEADER_STRINGKEY, out var playerUIDStr) == false)
                {
                    //ERROR
                    errorCode = MsgErrorCode.InvalidErrorCode;
                    return;
                }
                if (context.Request.Headers.TryGetValue(BKNetwork.ConstEnum.Consts.SESSION_ID_HTTP_HEADER_STRINGKEY, out var sessionIDStr) == false)
                {
                    //ERROR
                    errorCode = MsgErrorCode.InvalidErrorCode;
                    return;
                }
                if (BKServerBase.Config.ConfigManager.Instance.RedisConnectionConf == null)
                {
                    //ERROR
                    errorCode = MsgErrorCode.InvalidErrorCode;
                    return;
                }

                var redisDataService = context.RequestServices.GetService<RedisDataService>();
                if (redisDataService is null)
                {
                    throw new Exception($"redisDataService is empty");
                }
                
                var playerUID = Convert.ToInt64(playerUIDStr);
                var savedSessionID = await redisDataService.GetSessionIDAsync(playerUID);
                if (string.IsNullOrEmpty(savedSessionID))
                {
                    CoreLog.Critical.LogError($"Session expire occured. playerUID : {playerUID}");
                    errorCode = MsgErrorCode.ApiErrorSessionExpired;
                    return;
                }

                if (savedSessionID != sessionIDStr)
                {
                    CoreLog.Critical.LogError($"Invalid SessionID, playerUID: {playerUID}");
                    errorCode = MsgErrorCode.ApiErrorInvalidSessionID;
                    return;
                }
            }
            catch(Exception e)
            {
                CoreLog.Critical.LogError(e);
            }
            finally
            {
                if (errorCode != MsgErrorCode.Success)
                {
                    await HandleExceptionAsync(context, errorCode);
                }
                else
                {
                    await m_Next.Invoke(context);
                }
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, MsgErrorCode errorCode)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var errorAns = new IAPIResMsg(MsgType.Invalid)
            {
                errorCode = errorCode
            };
            var buffer = JsonConvert.SerializeObject(errorAns);
            //ERROR
            await context.Response.WriteAsync(buffer);
        }
    }
}
