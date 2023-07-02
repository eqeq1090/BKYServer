using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.Text;
using BKNetwork.Serialize;
using BKProtocol;

namespace BKWebAPIComponent.Formatter;
public class VillInputFormatter : TextInputFormatter
{
    public VillInputFormatter()
    {
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);

        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));

    }

    public override bool CanRead(InputFormatterContext context)
    {
        return base.CanRead(context);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context, Encoding effectiveEncoding)
    {
        var httpContext = context.HttpContext;
        var serviceProvider = httpContext.RequestServices;

        var logger = serviceProvider.GetRequiredService<ILogger<VillInputFormatter>>();

        using var reader = new StreamReader(httpContext.Request.Body, effectiveEncoding);
        
        try
        {
            var reqStr = await reader.ReadToEndAsync();
            if (reqStr == null)
            {
                return await InputFormatterResult.FailureAsync();
            }
            var jsonObj = JObject.Parse(reqStr);
            if (jsonObj == null)
            {
                return await InputFormatterResult.FailureAsync();
            }
            var msgTypeValue = (MsgType)jsonObj.Value<int>("msgType");
            if (msgTypeValue == MsgType.Invalid)
            {
                return await InputFormatterResult.FailureAsync();
            }

            var parsedMsg = MsgPairGenerator.Instance.Deserialize(msgTypeValue, reqStr);

            return await InputFormatterResult.SuccessAsync(parsedMsg);
        }
        catch
        {
            return await InputFormatterResult.FailureAsync();
        }
    }
}
