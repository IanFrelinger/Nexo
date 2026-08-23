namespace Ashlar.CLI.Commands.Workflow;

/// <summary>Target execution stats.</summary>
internal sealed class TargetExecutionStats
{
    /// <summary>Runs.</summary>
    public int Runs { get; set; }
    /// <summary>Successes.</summary>
    public int Successes { get; set; }
    /// <summary>Total latency ms.</summary>
    public long TotalLatencyMs { get; set; }
}
