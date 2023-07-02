using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Logger
{
    public enum LoggingEventType { Debug, Information, Warning, Error, Fatal };

    public interface ILogger
    {
        void Log(LogEntry entry);
    }
}
