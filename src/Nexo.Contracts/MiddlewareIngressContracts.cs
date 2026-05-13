namespace Nexo.Contracts;

/// <summary>Stable transport labels for logging, envelopes, and capability checks.</summary>
public static class NexoIngressTransports
{
    public const string Http = "http";
    public const string WebSocket = "websocket";
    public const string SmsSimulated = "sms-simulated";
}

/// <summary>
/// Cross-cutting ingress metadata adapters map into before invoking Nexo use cases.
/// Correlation id is always populated (generated or from <c>X-Correlation-Id</c>).
/// </summary>
public sealed record NexoIngressEnvelope(
    string CorrelationId,
    string Transport,
    string? IdempotencyKey = null,
    string? PayloadVersion = null,
    string? TenantId = null,
    string? AppId = null)
{
    public const string HttpContextItemKey = "Nexo.IngressEnvelope";
}

public sealed record SmsInboundSimulationRequest(
    string From,
    string Body,
    string? MessageSid = null,
    string? AppId = null);

public sealed record SmsInboundSimulationResponse(
    bool Accepted,
    string? ApprovalToken,
    string Message,
    bool IdempotentReplay);

/// <summary>Static inventory for operators (Phase A — ingress seams).</summary>
public sealed record IngressCatalogEntry(string Kind, string Route, string Notes);

public sealed record IngressCatalogResponse(IReadOnlyList<IngressCatalogEntry> EntryPoints);

/// <summary>Snapshot of the current HTTP ingress context (for operators and tests).</summary>
public sealed record IngressContextResponse(
    string? CorrelationId,
    string? Transport,
    string? TenantId,
    string? AppId,
    string? IdempotencyKey,
    string? PayloadVersion);
