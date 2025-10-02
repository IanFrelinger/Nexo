namespace NexoDirectorStudio.Orchestration
{
    public interface IMetrics { void Add(string name, double value); void Set(string name, double value); }
}
