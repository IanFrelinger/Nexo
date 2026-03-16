namespace Nexo.Abstractions.Transport;

/// <summary>
/// Request envelope for transport-level agent invocation.
/// </summary>
/// <param name="AgentName">Logical target agent name/id.</param>
/// <param name="CorrelationId">Cross-boundary correlation identifier for trace propagation.</param>
/// <param name="DependencyOutputs">Optional dependency outputs to provide as invocation context.</param>
/// <param name="Metadata">Optional metadata for transport implementations.</param>
public sealed record AgentInvocationRequest(
    string AgentName,
    string CorrelationId,
    IReadOnlyDictionary<string, object>? DependencyOutputs = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
