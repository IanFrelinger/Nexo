using Ashlar.Core.Domain.Clusters;

namespace Ashlar.Core.Domain.Workflows;

/// <summary>
/// A connection between cluster instances in a workflow.
/// </summary>
public class WorkflowConnection
{
    /// <summary>Stable connection identifier.</summary>
    public string Id { get; init; } = default!;

    /// <summary>Source cluster instance id.</summary>
    public string FromInstanceId { get; init; } = default!;

    /// <summary>Output port name on the source instance.</summary>
    public string FromOutput { get; init; } = default!;

    /// <summary>Target cluster instance id.</summary>
    public string ToInstanceId { get; init; } = default!;

    /// <summary>Input port name on the target instance.</summary>
    public string ToInput { get; init; } = default!;
}
