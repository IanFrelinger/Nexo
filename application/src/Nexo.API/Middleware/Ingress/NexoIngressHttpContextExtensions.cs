using Nexo.Contracts;

namespace Nexo.API.Middleware.Ingress;

/// <summary>HTTP helpers for adapters mapping into shared ingress metadata.</summary>
public static class NexoIngressHttpContextExtensions
{
    /// <summary>Gets the ingress envelope attached to the HTTP context, if present.</summary>
    public static NexoIngressEnvelope? GetIngressEnvelope(this HttpContext httpContext) =>
        httpContext.Items.TryGetValue(NexoIngressEnvelope.HttpContextItemKey, out var v) && v is NexoIngressEnvelope env
            ? env
            : null;
}
