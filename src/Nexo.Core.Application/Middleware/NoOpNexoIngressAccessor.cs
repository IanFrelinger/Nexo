using Nexo.Core.Application.Middleware.Ports;

namespace Nexo.Core.Application.Middleware;

/// <summary>Default <see cref="INexoIngressAccessor"/> when no HTTP request context exists (CLI, tests, workers).</summary>
public sealed class NoOpNexoIngressAccessor : INexoIngressAccessor
{
    public string? CorrelationId => null;

    public string? Transport => null;

    public string? TenantId => null;

    public string? AppId => null;

    public string? IdempotencyKey => null;

    public string? PayloadVersion => null;
}
