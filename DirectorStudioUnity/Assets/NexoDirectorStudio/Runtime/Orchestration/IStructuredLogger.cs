namespace NexoDirectorStudio.Orchestration
{
    public interface IStructuredLogger { void Info(string evt, object data=null); void Warn(string evt, object data=null); void Error(string evt, object data=null); }
}
