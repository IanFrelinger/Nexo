using System;
using System.IO;

namespace NexoDirectorStudio.Orchestration
{
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