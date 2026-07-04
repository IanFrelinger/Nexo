using Microsoft.AspNetCore.Http;
using Nexo.Core.Application.Middleware.Ports;

namespace Nexo.API.Middleware.Ingress;

/// <summary>
/// Maps ASP.NET Core <see cref="HttpContext"/> ingress items to <see cref="INexoIngressAccessor"/> for MediatR and handlers.
/// </summary>
public sealed class HttpNexoIngressAccessor(IHttpContextAccessor httpContextAccessor) : INexoIngressAccessor
{
    /// <inheritdoc />
    public string? CorrelationId
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext;
            return ctx?.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var c) == true && c is string s
                ? s
                : null;
        }
    }

    /// <inheritdoc />
    public string? Transport => GetEnvelope()?.Transport;

    /// <inheritdoc />
    public string? TenantId => GetEnvelope()?.TenantId;

    /// <inheritdoc />
    public string? AppId => GetEnvelope()?.AppId;

    /// <inheritdoc />
    public string? IdempotencyKey => GetEnvelope()?.IdempotencyKey;

    /// <inheritdoc />
    public string? PayloadVersion => GetEnvelope()?.PayloadVersion;

    private Nexo.Contracts.NexoIngressEnvelope? GetEnvelope() =>
        httpContextAccessor.HttpContext?.GetIngressEnvelope();
}
