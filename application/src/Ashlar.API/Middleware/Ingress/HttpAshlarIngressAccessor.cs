using Microsoft.AspNetCore.Http;
using Ashlar.Core.Application.Middleware.Ports;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>
/// Maps ASP.NET Core <see cref="HttpContext"/> ingress items to <see cref="IAshlarIngressAccessor"/> for MediatR and handlers.
/// </summary>
public sealed class HttpAshlarIngressAccessor(IHttpContextAccessor httpContextAccessor) : IAshlarIngressAccessor
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

    private Ashlar.Contracts.AshlarIngressEnvelope? GetEnvelope() =>
        httpContextAccessor.HttpContext?.GetIngressEnvelope();
}
