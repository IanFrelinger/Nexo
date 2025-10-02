using System;

namespace NexoDirectorStudio.Orchestration
{
    public sealed class SystemClock : IClock 
    { 
        public static readonly IClock Instance = new SystemClock(); 
        public DateTime UtcNow => DateTime.UtcNow; 
    }
}
