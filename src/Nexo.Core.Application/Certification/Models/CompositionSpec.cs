namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Declared brick graph for composition certification. Witness is provided separately.
/// </summary>
public sealed record CompositionSpec(
    string CompositionId,
    IReadOnlyList<CompositionNode> Nodes,
    IReadOnlyList<CompositionEdge> Edges,
    IReadOnlyList<CompositionPort> Inputs,
    IReadOnlyList<CompositionPort> Outputs);

public sealed record CompositionNode(string NodeId, string BrickId);

/// <summary>
/// Wires a producer port to a consumer port. Use <see cref="CompositionGraphConstants.InputNodeId"/>
/// / <see cref="CompositionGraphConstants.OutputNodeId"/> for composition-level I/O.
/// </summary>
public sealed record CompositionEdge(
    string SourceNodeId,
    string SourcePort,
    string TargetNodeId,
    string TargetPort);

public sealed record CompositionPort(string Name, string Type);

public static class CompositionGraphConstants
{
    public const string InputNodeId = "__input__";
    public const string OutputNodeId = "__output__";
}

/// <summary>
/// Independent end-to-end witness for a composition (not derived from constituent witnesses).
/// </summary>
public sealed record CompositionWitnessSpec(
    string CompositionId,
    IReadOnlyList<WitnessCase> Cases);
