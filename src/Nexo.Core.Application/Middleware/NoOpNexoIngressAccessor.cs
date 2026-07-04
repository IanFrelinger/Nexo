using Nexo.Core.Application.Middleware.Ports;

namespace Nexo.Core.Application.Middleware;

/// <summary>Default <see cref="INexoIngressAccessor"/> when no HTTP request context exists (CLI, tests, workers).</summary>
public sealed class NoOpNexoIngressAccessor : INexoIngressAccessor
{
    /// <inheritdoc />
    public string? CorrelationId => null;

    /// <inheritdoc />
    public string? Transport => null;

    /// <inheritdoc />
    public string? TenantId => null;

    /// <inheritdoc />
    public string? AppId => null;

    /// <inheritdoc />
    public string? IdempotencyKey => null;

    /// <inheritdoc />
    public string? PayloadVersion => null;
}
