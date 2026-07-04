namespace Nexo.Contracts;

/// <summary>Snapshot of the current HTTP ingress context (for operators and tests).</summary>
/// <param name="CorrelationId">Active request correlation id, when initialized.</param>
/// <param name="Transport">Active ingress transport label.</param>
/// <param name="TenantId">Resolved tenant id, when multi-tenant ingress is enabled.</param>
/// <param name="AppId">Resolved application id, when supplied by the caller.</param>
/// <param name="IdempotencyKey">Inbound idempotency key, when present.</param>
/// <param name="PayloadVersion">Inbound payload schema version, when present.</param>
public sealed record IngressContextResponse(
    string? CorrelationId,
    string? Transport,
    string? TenantId,
    string? AppId,
    string? IdempotencyKey,
    string? PayloadVersion);
