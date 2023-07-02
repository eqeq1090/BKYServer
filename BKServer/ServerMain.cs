using System;
using System.Threading;
using BKServerBase.Component;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Management;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKCommonComponent.Detail;
using BKCommonComponent.Node;
using BKCommonComponent.Redis;
using BKDataLoader.MasterData;
using BKGameServerComponent;
using BKNetwork.API;
using BKNetwork.Serialize;
using BKProtocol;
using BKWebAPIComponent;

namespace BKServer
{
    public sealed class ServerMain
    {
        public delegate void SigKillCallback();
        private SigKillCallback? m_SigkillCallback;

        private ComponentManager Component => ComponentManager.Instance;
        private ConfigManager ConfigManager => ConfigManager.Instance;

        public bool Initialize(SigKillCallback callback)
        {
            try
            {
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                m_SigkillCallback = callback;
                Component.SetHaltMethod(Halt);

                MsgPairGenerator.Instance.Init();
                TimeUtil.InitTickBase();
                ThreadPool.SetMinThreads(
                    ConfigManager.CommonConfig.WorkerThreadCountMin,
                    ConfigManager.CommonConfig.IOThreadCountMin);

                if (MasterDataManager.Instance.Initialize() == false)
                {
                    return false;
                }

                if (TryLoadComponent(ConfigManager.ServerMode) == false)
                {
                    return false;
                }

                CoreLog.Critical.LogFatal(
                    $"ServerMode: {ConfigManager.ServerMode}, " +
                    $"ServerProfile: {ConfigManager.ServerProfile}, " +
                    $"ServerId: {ConfigManager.ServerId}, " +
                    $"Started Successfully");
                return true;
            }
            catch (Exception e)
            {
                CoreLog.Critical.LogFatal($"ServerMain Initialize failed, {e.ToString()}");
                return false;
            }
        }

        public bool Shutdown()
        {
            CoreLog.Normal.LogError($"{ConfigManager.ServerMode} Module - Shutdown");
            Component.Stop();
            Thread.Sleep(2000);
            return true;
        }

        public void Halt()
        {
            m_SigkillCallback?.Invoke();
        }

        private bool TryLoadComponent(ServerMode serverMode)
        {
            try
            {
                Component.AddComponent<IRedisComponent>(new RedisComponent());

                switch (serverMode)
                {
                    case ServerMode.AllInOne:
                        Component.AddComponent<GameServerComponent>(GameServerComponent.Instance);
                        //Component.AddComponent<MatchServerComponent>(MatchServerComponent.Instance);
                        //Component.AddComponent<GlobalServerComponent>(GlobalServerComponent.Instance);
                        Component.AddComponent<ManagementComponent>(new ManagementComponent());
                        Component.AddComponent<APIDispatchComponent>(APIDispatchComponent.Instance);
                        //Component.AddComponent<APIServerComponent>(APIServerComponent.Instance);
                        break;

                    case ServerMode.MatchServer:
                        //Component.AddComponent<ManagementComponent>(new ManagementComponent());
                        //Component.AddComponent<MatchServerComponent>(MatchServerComponent.Instance);
                        //Component.AddComponent<APIDispatchComponent>(APIDispatchComponent.Instance);
                        break;

                    case ServerMode.GameServer:
                        Component.AddComponent<ManagementComponent>(new ManagementComponent());
                        Component.AddComponent<GameServerComponent>(GameServerComponent.Instance);
                        Component.AddComponent<APIDispatchComponent>(APIDispatchComponent.Instance);
                        break;

                    case ServerMode.APIServer:
                        Component.AddComponent<ManagementComponent>(new ManagementComponent());
                        Component.AddComponent<APIServerComponent>(APIServerComponent.Instance);
                        break;
                    //case ServerMode.GlobalServer:
                    //    Component.AddComponent<GlobalServerComponent>(GlobalServerComponent.Instance);
                    //    Component.AddComponent<APIDispatchComponent>(APIDispatchComponent.Instance);
                    //    Component.AddComponent<ManagementComponent>(new ManagementComponent());

                    default:
                        throw new InvalidOperationException($"not supported serverMode: {serverMode}");
                }

                if (false == Component.Initialize())
                {
                    CoreLog.Critical.LogFatal("Component Initializing Failed");
                    return false;
                }

                if (false == Component.LazyLoadComponent())
                {
                    CoreLog.Critical.LogFatal("Component Lazy Load Failed");
                    return false;
                }

                Component.InvokeWaitInitDone();
                Component.Start();
            }
            catch (Exception ex)
            {
                CoreLog.Critical.LogFatal(ex);
            }
            return true;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"OnUnhandledException");
            CoreLog.Critical.LogError($"Server died, OnUnhandleException, exception: {e.ToString()}");

            // TODO: dump.
        }

        private static void OnProcessExit(object? sender, EventArgs e)
        {
            Console.WriteLine($"OnProcessExit");
            CoreLog.Critical.LogError($"Server died, OnProcessExit");
        } // TODO: Redis key 삭제. (접속된 유저들만)
    }
}
