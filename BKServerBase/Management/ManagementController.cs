using BKServerBase.Logger;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using BKServerBase.Config;

namespace BKServerBase.Management
{
    public class ManagementController : WebApiController
    {
        [Route(HttpVerbs.Get, "/status")]
        public object Status()
        {
            return new
            {
                status = "OK"
            };
        }

        [Route(HttpVerbs.Get, "/config")]
        public object Config()
        {
            return new
            {
                Configuration = ConfigManager.Instance,
                Environment = Environment.GetEnvironmentVariables()
            };
        }

        [Route(HttpVerbs.Put, "/halt")]
        public object Halt()
        {
            //WorldComponent.Instance!.Halt("actuator");
            return new
            {
                status = "OK"
            };
        }

        [Route(HttpVerbs.Get, "/heapdump")]
        public async Task HeapDump()
        {
            var filename = $"(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).dmp";
            var path = Path.Combine(Path.GetTempPath(), filename);
            try
            {
                await using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
                {
                    if (MiniDump.Write(fs.SafeFileHandle, MiniDump.Option.WithFullMemory) == false)
                    {
                        throw new Exception("MiniDump. Write");
                    }
                }
                await using (var fs = new FileStream(path, System.IO.FileMode.Open))
                await using (var stream = HttpContext.OpenResponseStream())
                {
                    await fs.CopyToAsync(stream);
                }
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                }
            }
        }

        [Route(HttpVerbs.Put, "/reload/masterdata")]
        public object ReloadMasterData()
        {
         //   var result = WorldComponent.Instance!.ReloadMasterDataFrom();
            return new
            {
                status = "OK"
                //result = result.ToString()
            };
        }

        [Route(HttpVerbs.Put, "/reload/ai")]
        public object ReloadMonsterAI()
        {
            //    var result = WorldComponent.Instance!.ReloadMonsterAIFromLocal();
            return new
            {
                status = "OK"
                //result = result.ToString()
            };
        }

        /*
        [Route(HttpVerbs.Put, "/reload/config")]
        public object ReloadConfig()
        {
            ConfigurationManager.Instance!.Reload();
            return new
            {
                status = "OK"
            };
        }*/

        [Route(HttpVerbs.Put, "/gc")]
        public object GCCollect()
        {
            GC.Collect();
            return new
            {
                status = "OK"
            };
        }

        [Route(HttpVerbs.Put, "/kickall")]
        public object KickAl1()
        {
         //   WorldComponent.Instance.KickAll();
            return new
            {
                status = "OK"
            };
        }
    }
}
