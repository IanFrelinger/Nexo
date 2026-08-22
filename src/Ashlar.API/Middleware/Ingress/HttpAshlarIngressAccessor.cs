using Microsoft.AspNetCore.Http;
using Ashlar.Core.Application.Middleware.Ports;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>
/// Maps ASP.NET Core <see cref="HttpContext"/> ingress items to <see cref="IAshlarIngressAccessor"/> for MediatR and handlers.
/// </summary>
public sealed class HttpAshlarIngressAccessor(IHttpContextAccessor httpContextAccessor) : IAshlarIngressAccessor
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

    private Ashlar.Contracts.AshlarIngressEnvelope? GetEnvelope() =>
        httpContextAccessor.HttpContext?.GetIngressEnvelope();
}
