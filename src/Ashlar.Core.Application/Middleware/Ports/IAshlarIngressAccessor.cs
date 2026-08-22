namespace Ashlar.Core.Application.Middleware.Ports;

/// <summary>
/// Read-only view of HTTP ingress metadata for the current async context (when hosted in ASP.NET Core).
/// Non-web hosts use a no-op implementation returning nulls.
/// </summary>
public interface IAshlarIngressAccessor
{
    string? CorrelationId { get; }

    string? Transport { get; }

    string? TenantId { get; }

    string? AppId { get; }

    string? IdempotencyKey { get; }

    string? PayloadVersion { get; }
}
