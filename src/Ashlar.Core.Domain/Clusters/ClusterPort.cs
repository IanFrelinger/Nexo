using Ashlar.Core.Domain.Behaviors;

namespace Ashlar.Core.Domain.Clusters;

/// <summary>
/// Input or output port of a cluster.
/// </summary>
public class ClusterPort
{
    /// <summary>Port name exposed at the cluster boundary.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Data type label for the port value.</summary>
    public string Type { get; init; } = default!;

    /// <summary>Human-readable description of the port purpose.</summary>
    public string Description { get; init; } = default!;

    /// <summary>Whether a value must be provided for this port.</summary>
    public bool Required { get; init; } = true;

    /// <summary>Default value when the port is optional and not supplied.</summary>
    public object? Default { get; init; }

    /// <summary>
    /// Which internal brick and port this maps to.
    /// Format: "brickLocalId.portName"
    /// </summary>
    public string InternalMapping { get; init; } = default!;
}
