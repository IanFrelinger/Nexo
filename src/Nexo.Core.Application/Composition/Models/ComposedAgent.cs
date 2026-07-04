namespace Nexo.Core.Application.Composition.Models;

/// <summary>
/// A composed agent: ordered pipeline of component IDs.
/// No dynamic code generation; composition = ordered list of component IDs.
/// </summary>
public record ComposedAgent
{
    /// <summary>Stable identifier for the composed agent.</summary>
    public required string Id { get; init; }

    /// <summary>Problem statement this composition addresses.</summary>
    public required string ProblemDescription { get; init; }

    /// <summary>Ordered component identifiers forming the pipeline.</summary>
    public required IReadOnlyList<string> ComponentIds { get; init; }
}
