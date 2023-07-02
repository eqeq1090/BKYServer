using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using BKServer;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Util;

namespace BKServer;

class Program
{
    private static readonly ManualResetEvent m_TerminatedEvent = new ManualResetEvent(false);
    static void Main(string[] args)
    {
        Thread.CurrentThread.Name = "Main";

        try
        {
            // Interrupt Key Handle
            Console.CancelKeyPress += CancelKeyPressHandler;
            //config 초기화
            ConfigManager.Instance.Initialize(args);
            KeyGenerator.Initialize(ConfigManager.Instance.ServerId);
            ShowTitle(ConfigManager.Instance.ServerMode);

            var serverMain = new ServerMain();
            if (serverMain.Initialize(SigKill) == false)
            {
                Environment.Exit(1);
                return;
            }

            m_TerminatedEvent.WaitOne();
            bool isGracefulShutdown = serverMain.Shutdown();
            if (isGracefulShutdown)
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                Environment.Exit(2);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            Environment.Exit(3);
        }
        return;
    }

    private static void ShowTitle(ServerMode serverMode)
    {
        Console.Title = $"bk{serverMode}";

        if (BuildVersion.IsEnabled)
        {
            CoreLog.Normal.LogInfo($"{serverMode} GitCommitHash: {BuildVersion.GitCommitHash} BranchName: {BuildVersion.GitBranchName}" +
                $"BuildTime:{BuildVersion.BuildTime} BuildNumber: {BuildVersion.BuildNumber}");
            try
            {
                Console.Title = $"bk{serverMode} ({BuildVersion.BuildTime}/{BuildVersion.GitBranchName} /{BuildVersion.GitCommitHash})";
            }
            catch (Exception)
            {
            }
        }
    }

    private static void CancelKeyPressHandler(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        m_TerminatedEvent.Set();
    }

    private static void SigKill()
    {
        m_TerminatedEvent.Set();
    }

}



