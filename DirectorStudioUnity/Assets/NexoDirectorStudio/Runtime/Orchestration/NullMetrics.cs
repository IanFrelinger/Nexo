namespace NexoDirectorStudio.Orchestration
{
    public sealed class NullMetrics : IMetrics 
    { 
        public void Add(string name, double value){} 
        public void Set(string name, double value){} 
    }
}
