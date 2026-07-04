namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Declared brick graph for composition certification. Witness is provided separately.
/// </summary>
/// <param name="CompositionId">Unique composition identifier.</param>
/// <param name="Nodes">Brick nodes in the composition graph.</param>
/// <param name="Edges">Port wiring between nodes and composition I/O.</param>
/// <param name="Inputs">Composition-level input ports.</param>
/// <param name="Outputs">Composition-level output ports.</param>
public sealed record CompositionSpec(
    string CompositionId,
    IReadOnlyList<CompositionNode> Nodes,
    IReadOnlyList<CompositionEdge> Edges,
    IReadOnlyList<CompositionPort> Inputs,
    IReadOnlyList<CompositionPort> Outputs);
