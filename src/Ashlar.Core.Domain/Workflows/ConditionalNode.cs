using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Clusters;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Domain.Workflows;

/// <summary>
/// Conditional node for branching logic.
/// </summary>
public record ConditionalNode : WorkflowNode
{
    /// <summary>Boolean expression controlling downstream branch activation.</summary>
    public string Condition { get; init; } = "";
}
