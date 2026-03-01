namespace Nexo.Core.Application.Composition.Models;

/// <summary>
/// A composed agent: ordered pipeline of component IDs.
/// No dynamic code generation; composition = ordered list of component IDs.
/// </summary>
public record ComposedAgent
{
    public required string Id { get; init; }
    public required string ProblemDescription { get; init; }
    public required IReadOnlyList<string> ComponentIds { get; init; }
}
