using Ashlar.Contracts;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>
/// Builds a transport-agnostic <see cref="AshlarIngressEnvelope"/> for HTTP requests (WebSocket + SMS adapters set their own).
/// </summary>
public sealed class IngressEnvelopeMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var correlation =
            context.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var c) && c is string s
                ? s
                : Guid.NewGuid().ToString("N");

        var idempotency = context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        var payloadVersion = context.Request.Headers["X-Ashlar-Payload-Version"].FirstOrDefault();
        var tenantId = context.Request.Headers["X-Ashlar-Tenant"].FirstOrDefault();
        var appId = context.Request.Headers["X-Ashlar-App-Id"].FirstOrDefault();

        context.Items[AshlarIngressEnvelope.HttpContextItemKey] = new AshlarIngressEnvelope(
            correlation,
            AshlarIngressTransports.Http,
            string.IsNullOrWhiteSpace(idempotency) ? null : idempotency.Trim(),
            string.IsNullOrWhiteSpace(payloadVersion) ? null : payloadVersion.Trim(),
            string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim(),
            string.IsNullOrWhiteSpace(appId) ? null : appId.Trim());

        return next(context);
    }
}
