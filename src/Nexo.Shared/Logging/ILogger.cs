using System;

namespace Nexo.Shared.Logging
{
    /// <summary>
    /// Logger interface
    /// </summary>
    public interface ILogger
    {
        void Log(LogEntry entry);
    }
}
