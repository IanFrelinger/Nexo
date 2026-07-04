using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Workflows;

/// <summary>
/// Transform node for data manipulation.
/// </summary>
public record TransformNode : WorkflowNode
{
    /// <summary>Transformation operation to apply to upstream data.</summary>
    public TransformOperation Operation { get; init; } = TransformOperation.Map;

    /// <summary>Expression evaluated by the transform engine.</summary>
    public string Expression { get; init; } = "";
}
