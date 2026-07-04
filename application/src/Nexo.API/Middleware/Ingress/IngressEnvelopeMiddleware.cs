using Nexo.Contracts;

namespace Nexo.API.Middleware.Ingress;

/// <summary>
/// Builds a transport-agnostic <see cref="NexoIngressEnvelope"/> for HTTP requests (WebSocket + SMS adapters set their own).
/// </summary>
public sealed class IngressEnvelopeMiddleware(RequestDelegate next)
{
    /// <summary>Builds an HTTP ingress envelope from request headers and correlation context.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        var correlation =
            context.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var c) && c is string s
                ? s
                : Guid.NewGuid().ToString("N");

        var idempotency = context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        var payloadVersion = context.Request.Headers["X-Nexo-Payload-Version"].FirstOrDefault();
        var tenantId = context.Request.Headers["X-Nexo-Tenant"].FirstOrDefault();
        var appId = context.Request.Headers["X-Nexo-App-Id"].FirstOrDefault();

        context.Items[NexoIngressEnvelope.HttpContextItemKey] = new NexoIngressEnvelope(
            correlation,
            NexoIngressTransports.Http,
            string.IsNullOrWhiteSpace(idempotency) ? null : idempotency.Trim(),
            string.IsNullOrWhiteSpace(payloadVersion) ? null : payloadVersion.Trim(),
            string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim(),
            string.IsNullOrWhiteSpace(appId) ? null : appId.Trim());

        return next(context);
    }
}
