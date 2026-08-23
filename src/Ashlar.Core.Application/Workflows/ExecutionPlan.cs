namespace Ashlar.Core.Application.Workflows;

/// <summary>Resolved execution order for a visual workflow graph.</summary>
public class ExecutionPlan
{
    /// <summary>Topologically sorted node ids to execute sequentially.</summary>
    public List<string> ExecutionOrder { get; init; } = new();

    /// <summary>Node groups that may execute in parallel within each inner list.</summary>
    public List<List<string>> ParallelGroups { get; init; } = new();
}
