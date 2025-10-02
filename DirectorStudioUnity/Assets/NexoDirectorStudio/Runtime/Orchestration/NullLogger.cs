namespace NexoDirectorStudio.Orchestration
{
    public sealed class NullLogger : IStructuredLogger
    { 
        public void Info(string evt, object data=null){} 
        public void Warn(string evt, object data=null){} 
        public void Error(string evt, object data=null){} 
    }
}
