using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;
using BKServerBase.Logger;
using BKProtocol;

namespace BKWebAPIComponent.Middleware
{
    public class BKDeveloperExceptionHandlerMiddleware
    {
        private readonly RequestDelegate m_Next;

        public BKDeveloperExceptionHandlerMiddleware(RequestDelegate next)
        {
            m_Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await m_Next.Invoke(context);
            }
            catch(Exception ex)
            {
                CoreLog.Critical.LogError(ex);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new IAPIResMsg(MsgType.Invalid)
                {
                    errorCode = MsgErrorCode.ApiErrorExceptionOccurred
                }));
            }
        }
    }
}
