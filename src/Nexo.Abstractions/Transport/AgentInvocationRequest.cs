namespace Nexo.Abstractions.Transport;

/// <summary>
/// Request envelope for transport-level agent invocation.
/// </summary>
/// <param name="AgentName">Logical target agent name/id.</param>
/// <param name="CorrelationId">Cross-boundary correlation identifier for trace propagation.</param>
/// <param name="SpanId">Optional distributed trace span identifier.</param>
/// <param name="Payload">Optional payload to provide as invocation context.</param>
/// <param name="Options">Optional invocation options (timeout/retries/target endpoint).</param>
/// <param name="Metadata">Optional metadata for transport implementations.</param>
/// <param name="DependencyOutputs">
/// Backward-compatible dependency outputs map. Prefer <paramref name="Payload"/> for new call sites.
/// </param>
public sealed record AgentInvocationRequest(
    string AgentName,
    string CorrelationId,
    string? SpanId = null,
    IReadOnlyDictionary<string, object?>? Payload = null,
    AgentInvocationOptions? Options = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    IReadOnlyDictionary<string, object>? DependencyOutputs = null)
{
    /// <summary>
    /// Effective invocation options with defaults applied.
    /// </summary>
    public AgentInvocationOptions EffectiveOptions => Options ?? AgentInvocationOptions.Default;
}
