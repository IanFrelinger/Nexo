using Microsoft.AspNetCore.Http;
using Nexo.Core.Application.Middleware.Ports;

namespace Nexo.API.Middleware.Ingress;

/// <summary>
/// Maps ASP.NET Core <see cref="HttpContext"/> ingress items to <see cref="INexoIngressAccessor"/> for MediatR and handlers.
/// </summary>
public sealed class HttpNexoIngressAccessor(IHttpContextAccessor httpContextAccessor) : INexoIngressAccessor
{
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

    public string? Transport => GetEnvelope()?.Transport;

    public string? TenantId => GetEnvelope()?.TenantId;

    public string? AppId => GetEnvelope()?.AppId;

    public string? IdempotencyKey => GetEnvelope()?.IdempotencyKey;

    public string? PayloadVersion => GetEnvelope()?.PayloadVersion;

    private Nexo.Contracts.NexoIngressEnvelope? GetEnvelope() =>
        httpContextAccessor.HttpContext?.GetIngressEnvelope();
}
