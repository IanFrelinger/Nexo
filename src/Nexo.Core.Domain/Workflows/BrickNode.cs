using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Workflows;

/// <summary>
/// A standalone brick node (not part of an agent).
/// </summary>
public record BrickNode : WorkflowNode
{
    /// <summary>Catalog brick id to execute.</summary>
    public string BrickId { get; init; } = "";

    /// <summary>Preferred brick implementation for this node.</summary>
    public ImplementationType Implementation { get; init; } = ImplementationType.Auto;

    /// <summary>Optional LLM provider override for agentic implementations.</summary>
    public string? ProviderOverride { get; init; }

    /// <summary>Input parameter values bound to the brick interface.</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; }
        = new Dictionary<string, object>();
}
