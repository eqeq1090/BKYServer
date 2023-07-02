using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Config;
using Microsoft.Extensions.Logging;
using BKServerBase.Util;

namespace BKServerBase.Logger
{
    public class LogEntry
    {
        public readonly LoggingEventType Severity;
        public readonly string Message;
        public readonly Exception? Exception;
        public LogEntry(LoggingEventType severity, string message, string filePath, int line, Exception? exception = null)
        {
            if (null == message)
            {
                throw new ArgumentNullException("message");
            }
            if (0 == message.Length)
            {
                throw new ArgumentException("empty", "message");
            }
            if (exception != null)
            {
                message = $"{message} {exception.StackTrace} [{TagBuilder.MakeTag(filePath, line)}]";
            }
            else
            {
                message = $"{message} [{TagBuilder.MakeTag(filePath, line)}]";
            }
            if (ConfigManager.Instance.LogLevelOverride.TryGetValue($"{TagBuilder.MakeTag(filePath, line)}", out var overridedLevel))
            {
                Severity = overridedLevel;
            }
            else
            {
                Severity = severity;
            }
            Message = message;
            Exception = exception;
        }
    }

    public class MiscLog
    {
        public static ILogger Normal = new NLogAdapter("Core");
        public static ILogger Critical = new NLogAdapter("Critical");
    }

    public class CoreLog
    {
        public static ILogger Normal = new NLogAdapter("Core");
        public static ILogger Critical = new NLogAdapter("Critical");
    }

    public class GameNetworkLog
    {
        public static ILogger Normal = new NLogAdapter("Network");
        public static ILogger Critical = new NLogAdapter("Network");
    }

    public class APINetworkLog
    {
        public static ILogger Normal = new NLogAdapter("Network");
        public static ILogger Critical = new NLogAdapter("Network");
    }

    public class MatchNetworkLog
    {
        public static ILogger Normal = new NLogAdapter("Network");
        public static ILogger Critical = new NLogAdapter("Network");
    }

    public class ContentsLog
    {
        public static ILogger Normal = new NLogAdapter("Contents");
        public static ILogger Critical = new NLogAdapter("Contents");
    }

    public class BotClientLog
    {
        public static ILogger Normal = new NLogAdapter("BotClient");
        public static ILogger Critical = new NLogAdapter("BotClient");
    }

    public static class LogExtensions
    {
        public static void LogDebug(this ILogger logger, string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Debug, message, filePath, line));
        }

        public static void LogDebug(this ILogger logger, Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Debug, exception.Message, filePath, line, exception));
        }

        public static void LogInfo(this ILogger logger, string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Information, message, filePath, line));
        }

        public static void LogInfo(this ILogger logger, Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Information, exception.Message, filePath, line, exception));
        }

        public static void Log(this ILogger logger, Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Information, exception.Message, filePath, line, exception));
        }

        public static void LogWarning(this ILogger logger, string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Warning, message, filePath, line));
        }

        public static void LogWarning(this ILogger logger, Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Warning, exception.Message, filePath, line, exception));
        }
        public static void LogError(this ILogger logger, string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, message, filePath, line));
        }

        public static void LogError(this ILogger logger, Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, exception.Message, filePath, line, exception));
        }

        public static void LogFatal(this ILogger logger, string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Fatal, message, filePath, line));
        }

        public static void LogFatal(this ILogger logger, Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            logger.Log(new LogEntry(LoggingEventType.Fatal, exception.Message, filePath, line, exception));
        }
    }

    // NOTE(OSCAR) This is temporary. The reason is that I developed some code that uses `ILogger` but the rest of the codebase doesnt. So
    // for now I need to wrap our logger in this.
    public sealed class TemporaryLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly ILogger _realLogger;
        private readonly bool _isLogging;

        public TemporaryLogger(ILogger realLogger, bool isLogging)
        {
            _realLogger = realLogger;
            _isLogging = isLogging;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (_isLogging is false)
            {
                return;
            }

            switch (logLevel) {
                case LogLevel.Trace: {
                        if (exception != null)
                        {
                            _realLogger.LogDebug(exception);
                        }
                        else
                        {
                            _realLogger.LogDebug($"{formatter(state, exception)}");
                        }
                    } break;
                case LogLevel.Debug: {
                    if (exception != null) {
                        _realLogger.LogDebug(exception);
                    }
                    else {
                        _realLogger.LogDebug($"{formatter(state, exception)}");
                    }
                } break;
                case LogLevel.Information: {
                    if (exception != null) {
                        _realLogger.LogInfo(exception);
                    }
                    else {
                        _realLogger.LogInfo($"{formatter(state, exception)}");
                    }
                } break;
                case LogLevel.Warning: {
                    if (exception != null) {
                        _realLogger.LogWarning(exception);
                    }
                    else {
                        _realLogger.LogWarning($"{formatter(state, exception)}");
                    }
                } break;
                case LogLevel.Error: {
                    if (exception != null) {
                        _realLogger.LogError(exception);
                    }
                    else {
                        _realLogger.LogError($"{formatter(state, exception)}");
                    }
                } break;
                case LogLevel.Critical: {
                    if (exception != null) {
                        _realLogger.LogFatal(exception);
                    }
                    else {
                        _realLogger.LogFatal($"{formatter(state, exception)}");
                    }
                } break;
                case LogLevel.None: {
                } break;
            }
        }
    }

}
