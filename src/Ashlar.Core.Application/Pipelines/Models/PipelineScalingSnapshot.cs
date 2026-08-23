namespace Ashlar.Core.Application.Pipelines.Models;

/// <summary>
/// Snapshot for scaling policy input.
/// </summary>
public sealed record PipelineScalingSnapshot
{
    /// <summary>Number of pending jobs in the deterministic worker queue.</summary>
    public int DeterministicQueueDepth { get; init; }

    /// <summary>Number of pending jobs in the agentic worker queue.</summary>
    public int AgenticQueueDepth { get; init; }

    /// <summary>Recent error rate for deterministic workers (0.0 to 1.0).</summary>
    public double DeterministicErrorRate { get; init; }

    /// <summary>Recent error rate for agentic workers (0.0 to 1.0).</summary>
    public double AgenticErrorRate { get; init; }

    /// <summary>Currently active deterministic worker count.</summary>
    public int CurrentDeterministicWorkers { get; init; }

    /// <summary>Currently active agentic worker count.</summary>
    public int CurrentAgenticWorkers { get; init; }
}
