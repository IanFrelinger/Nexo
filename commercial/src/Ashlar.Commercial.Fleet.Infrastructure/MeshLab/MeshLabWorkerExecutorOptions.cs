namespace Ashlar.Commercial.Fleet.Infrastructure.MeshLab;

/// <summary>
/// Virtual mesh lab only: polls a remote director and completes assigned tasks without manual PATCH.
/// </summary>
public sealed class MeshLabWorkerExecutorOptions
{
    /// <summary>Constant value for section path.</summary>
    public const string SectionPath = "Ashlar:MeshLab:WorkerExecutor";

    /// <summary>When true, <see cref="MeshLabWorkerExecutorBackgroundService"/> runs in this API process.</summary>
    public bool Enabled { get; set; }

    /// <summary>Director base URL (e.g. <c>http://peer-a:8080</c> in Compose).</summary>
    public string DirectorBaseUrl { get; set; } = "http://127.0.0.1:18081";

    /// <summary>API key for director and assigned-peer calls; falls back to <c>Ashlar:Security:ApiKey</c> when empty.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Only tasks whose <see cref="MeshLabTaskSnapshot.Name"/> starts with this prefix are executed.</summary>
    public string TaskNamePrefix { get; set; } = "mesh-lab-worker-exec";

    /// <summary>Poll interval when idle.</summary>
    public int PollIntervalMs { get; set; } = 500;

    /// <summary>When true, POST <c>/api/bricks/{id}/execute</c> on the assigned peer before Succeeded.</summary>
    public bool ExecuteBrickOnAssignedPeer { get; set; } = true;

    /// <summary>Written to <c>resultSummary</c> on success.</summary>
    public string ResultSummary { get; set; } = "mesh-lab-worker-executor-complete";
}
