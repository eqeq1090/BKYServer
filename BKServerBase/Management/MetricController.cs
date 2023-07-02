using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System.Threading.Tasks;
using Prometheus;

namespace BKServerBase.Management
{
    public class MetricsController : WebApiController
    {
        [Route(HttpVerbs.Get, "/prometheus")]
        public async Task HeapDump()
        {
            Response.ContentType = PrometheusConstants.ExporterContentType;
            Response.StatusCode = 200;
            await using (var stream = HttpContext.OpenResponseStream())
            {
                await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            }
        }
    }
}

