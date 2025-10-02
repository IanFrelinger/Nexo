using System;
using System.IO;

namespace NexoDirectorStudio.Orchestration
{
    public interface IClock { DateTime UtcNow { get; } }
    public sealed class SystemClock : IClock { public static readonly IClock Instance = new SystemClock(); public DateTime UtcNow => DateTime.UtcNow; }

    public interface IRng { int Next(); float Next01(); void Init(int seed); }
    public sealed class DefaultRng : IRng
    {
        private Random _r = new Random();
        public void Init(int seed){ _r = new Random(seed); UnityEngine.Random.InitState(seed); }
        public int Next() => _r.Next();
        public float Next01() => (float)_r.NextDouble();
    }

    public interface IStructuredLogger { void Info(string evt, object data=null); void Warn(string evt, object data=null); void Error(string evt, object data=null); }
    public sealed class NullLogger : IStructuredLogger
    { public void Info(string evt, object data=null){} public void Warn(string evt, object data=null){} public void Error(string evt, object data=null){} }

    public interface IMetrics { void Add(string name, double value); void Set(string name, double value); }
    public sealed class NullMetrics : IMetrics { public void Add(string name, double value){} public void Set(string name, double value){} }

    public interface IAdapters { /* Ollama/Piper/ComfyUI etc. expose via interfaces in your project */ }

    public interface ICheckpointStore
    {
        System.Threading.Tasks.Task SaveAsync(PhaseToken token, object output, RunContext ctx);
        System.Threading.Tasks.Task<(bool found, object data)> TryLoadAsync(PhaseToken token, RunContext ctx);
    }

    [Serializable]
    public sealed class RunContext
    {
        public Guid RunId { get; } = Guid.NewGuid();
        public int Seed { get; init; } = 1337;
        public string ArtifactRoot { get; init; } = "Artifacts";
        public IClock Clock { get; init; } = SystemClock.Instance;
        public IRng Rng { get; } = new DefaultRng();
        public IStructuredLogger Log { get; init; } = new NullLogger();
        public IMetrics Metrics { get; init; } = new NullMetrics();
        public IAdapters Adapters { get; init; }
        public ICheckpointStore Checkpoints { get; init; }

        public string RunFolder => Path.Combine(ArtifactRoot, RunId.ToString("N"));
        public string PhaseFolder(PhaseToken token) => Path.Combine(RunFolder, token.Value);
    }
}
