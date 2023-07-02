using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using BKNetwork.Serialize;
using BKProtocol;

namespace BKWebAPIComponent.Formatter;

public class VillOutputFormatter : TextOutputFormatter
// </snippet_ClassDeclaration>
{
    // <snippet_ctor>
    public VillOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    public override async Task WriteResponseBodyAsync(
        OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var httpContext = context.HttpContext;
        var serviceProvider = httpContext.RequestServices;

        var logger = serviceProvider.GetRequiredService<ILogger<VillOutputFormatter>>();

        if (context.Object is not IAPIResMsg resMsg)
        {
            //ERROR
            httpContext.Response.StatusCode = 500;
            await httpContext.Response.WriteAsync("", selectedEncoding);
            return;
        }

        var buffer = JsonConvert.SerializeObject(resMsg); //MsgPairGenerator.Instance.Serialize(resMsg);
        await httpContext.Response.WriteAsync(buffer, selectedEncoding);
    }
}