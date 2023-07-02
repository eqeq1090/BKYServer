using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using BKServerBase.Config;

namespace BKServerBase.Logger
{
    public class NLogAdapter : ILogger, IDisposable
    {
        private readonly NLog.Logger LoggerAdaptee;

        public NLogAdapter(string loggerName = "Main", string logConfigFileName = "nlog.config")
        {
            var asm = Assembly.GetEntryAssembly();
            var asmName = asm?.GetName().ToString()??string.Empty;
            if (asmName.Contains("client") == false)
            {
                var profile = ConfigManager.Instance.ServerProfile;
                if (profile != ConstEnum.ServerProfile.Dev &&
                    profile != ConstEnum.ServerProfile.Local)
                {
                    logConfigFileName = logConfigFileName.Replace(".xml", $"_{profile.ToString()}.xml");
                }
            }
            LoggerAdaptee = NLog.LogManager.GetLogger(loggerName);
        }

        public void Log(LogEntry entry)
        {
            //Here invoke mAdaptee
            if (LoggingEventType.Debug == entry.Severity)
            {
                LoggerAdaptee.Debug(entry.Message, entry.Exception);
            }
            else if (LoggingEventType.Information == entry.Severity)
            {
                LoggerAdaptee.Info(entry.Message, entry.Exception);
            }
            else if (LoggingEventType.Warning == entry.Severity)
            {
                LoggerAdaptee.Warn(entry.Message, entry.Exception);
            }
            else if (LoggingEventType.Error == entry.Severity)
            {
                LoggerAdaptee.Error(entry.Message, entry.Exception);
            }
            else
            {
                LoggerAdaptee.Fatal(entry.Message, entry.Exception);
            }
        }

        public void Dispose()
        {

        }
    }
}
