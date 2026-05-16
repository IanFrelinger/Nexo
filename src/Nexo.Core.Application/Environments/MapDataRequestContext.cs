namespace Nexo.Core.Application.Environments;

/// <summary>
/// Cross-cutting context passed to map data providers (tracing, auth hints, feature flags).
/// </summary>
/// <param name="CorrelationId">Optional request correlation id for logs.</param>
/// <param name="Hints">Opaque hints for concrete providers only.</param>
public sealed record MapDataRequestContext(
    string? CorrelationId = null,
    IReadOnlyDictionary<string, string?>? Hints = null);
