namespace Nexo.Contracts;

/// <summary>
/// Cross-cutting ingress metadata adapters map into before invoking Nexo use cases.
/// Correlation id is always populated (generated or from <c>X-Correlation-Id</c>).
/// </summary>
/// <param name="CorrelationId">Request correlation id for tracing and idempotent replay.</param>
/// <param name="Transport">Ingress transport label; see <see cref="NexoIngressTransports"/>.</param>
/// <param name="IdempotencyKey">Optional idempotency key for deduplicating inbound requests.</param>
/// <param name="PayloadVersion">Optional wire-format or schema version of the inbound payload.</param>
/// <param name="TenantId">Optional tenant identifier for multi-tenant hosts.</param>
/// <param name="AppId">Optional application identifier for SMS or mobile ingress flows.</param>
public sealed record NexoIngressEnvelope(
    string CorrelationId,
    string Transport,
    string? IdempotencyKey = null,
    string? PayloadVersion = null,
    string? TenantId = null,
    string? AppId = null)
{
    /// <summary>
    /// Key used to store this envelope in the current HTTP request context item bag.
    /// </summary>
    public const string HttpContextItemKey = "Nexo.IngressEnvelope";
}
