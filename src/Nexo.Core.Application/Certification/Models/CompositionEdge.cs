namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Wires a producer port to a consumer port. Use <see cref="CompositionGraphConstants.InputNodeId"/>
/// / <see cref="CompositionGraphConstants.OutputNodeId"/> for composition-level I/O.
/// </summary>
/// <param name="SourceNodeId">Producer node identifier.</param>
/// <param name="SourcePort">Output port name on the producer.</param>
/// <param name="TargetNodeId">Consumer node identifier.</param>
/// <param name="TargetPort">Input port name on the consumer.</param>
public sealed record CompositionEdge(
    string SourceNodeId,
    string SourcePort,
    string TargetNodeId,
    string TargetPort);
