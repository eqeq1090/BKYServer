using BKServerBase.Component;
using BKServerBase.Logger;
using EmbedIO;
using EmbedIO.WebApi;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BKServerBase.Config;

namespace BKServerBase.Management
{
    public class ManagementComponent : IComponent
    {
        private WebServer? m_webServer;
        private CancellationTokenSource? m_cancellationTokenSource;
        private Task? m_webServerTask;

        public ManagementComponent()
        {
            
        }

        public (bool success, OnComponentInitializedHandler? InitDoneFunc) Initialize()
        {             
            try
            {
                m_cancellationTokenSource = new CancellationTokenSource();
                //TODO 제대로된 포트로 Read하게 변경 요망
                var managementPort = ConfigManager.Instance.GetManagementPort();
                CoreLog.Critical.LogInfo($"ServerManagementPort : {managementPort}");
                Swan.Logging.Logger.UnregisterLogger<Swan.Logging.ConsoleLogger>();

                m_webServer = new WebServer(managementPort);
                m_webServer.WithWebApi("/actuator", (m) =>
                {
                    m.WithController<ManagementController>();
                    m.WithController<MetricsController>();
                });

                CoreLog.Critical.LogInfo("ManagementComponent Initialized Successfully.");

                //Prometheus.DotNetRuntime.DotNetRuntimeStatsBuilder.Default().StartCollecting();
            }
            catch (SocketException)
            {
                EmbedIO.Net.EndPointManager.UseIpv6 = false;
                CoreLog.Critical.LogError("This system is not supported ipv6.");
            }
            catch (Exception ex)
            {
                CoreLog.Critical.LogError($"ManagementComponent ctor failed. {ex}");
            }
            return (true, WaitInitDone);
        }

        private void WaitInitDone()
        {
            if (m_cancellationTokenSource == null)
            {
                return;
            }
            m_webServerTask = m_webServer!.RunAsync(m_cancellationTokenSource.Token);
        }

        public bool Shutdown()
        {
            m_cancellationTokenSource?.Cancel();
            m_webServerTask!.Wait();
            return true;
        }

        public bool OnUpdate(double delta)
        {
            return true;
        }
    }
}