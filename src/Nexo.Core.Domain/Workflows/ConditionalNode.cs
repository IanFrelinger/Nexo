using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Workflows;

/// <summary>
/// Conditional node for branching logic.
/// </summary>
public record ConditionalNode : WorkflowNode
{
    /// <summary>Boolean expression controlling downstream branch activation.</summary>
    public string Condition { get; init; } = "";
}
