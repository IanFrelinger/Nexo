using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Clusters;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Domain.Workflows;

/// <summary>
/// A port on a node for connecting to other nodes.
/// </summary>
public record NodePort
{
    /// <summary>Unique port identifier within the parent node.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Display name shown on the node in the composer.</summary>
    public string Name { get; init; } = "";

    /// <summary>Whether this port receives or emits data.</summary>
    public PortDirection Direction { get; init; }

    /// <summary>Logical data type label (e.g. <c>string</c>, <c>any</c>).</summary>
    public string DataType { get; init; } = "any";

    /// <summary>Whether a connection must be attached before execution.</summary>
    public bool Required { get; init; } = true;

    /// <summary>Whether multiple connections may attach to this port.</summary>
    public bool AllowMultiple { get; init; } = false;
}
