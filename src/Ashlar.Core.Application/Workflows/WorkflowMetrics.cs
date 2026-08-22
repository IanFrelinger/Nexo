namespace Ashlar.Core.Application.Workflows;

/// <summary>Aggregate metrics for a workflow execution.</summary>
public class WorkflowMetrics
{
    /// <summary>Total wall-clock duration of the workflow run.</summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>Number of nodes executed.</summary>
    public int NodesExecuted { get; init; }

    /// <summary>Count of deterministic brick executions.</summary>
    public int DeterministicBricks { get; init; }

    /// <summary>Count of agentic brick executions.</summary>
    public int AgenticBricks { get; init; }

    /// <summary>Total LLM tokens consumed across agentic steps.</summary>
    public long TotalTokensUsed { get; init; }

    /// <summary>Number of semantic cache hits during execution.</summary>
    public int CacheHits { get; init; }
}
