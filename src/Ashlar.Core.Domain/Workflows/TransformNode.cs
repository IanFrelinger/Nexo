using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Clusters;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Domain.Workflows;

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
