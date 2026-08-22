using Ashlar.Contracts;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>HTTP helpers for adapters mapping into shared ingress metadata.</summary>
public static class AshlarIngressHttpContextExtensions
{
    public static AshlarIngressEnvelope? GetIngressEnvelope(this HttpContext httpContext) =>
        httpContext.Items.TryGetValue(AshlarIngressEnvelope.HttpContextItemKey, out var v) && v is AshlarIngressEnvelope env
            ? env
            : null;
}
