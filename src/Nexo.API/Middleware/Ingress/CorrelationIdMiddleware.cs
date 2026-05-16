using System.Diagnostics;

namespace Nexo.API.Middleware.Ingress;

/// <summary>
/// Accepts or generates <c>X-Correlation-Id</c>, exposes it on <see cref="HttpContext.Items"/> and response headers,
/// and tags the current <see cref="Activity"/> when present.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string HttpContextItemKey = "Nexo.CorrelationId";

    public Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var id = string.IsNullOrWhiteSpace(incoming) ? Guid.NewGuid().ToString("N") : incoming.Trim();

        context.Items[HttpContextItemKey] = id;
        context.Response.Headers.Append(HeaderName, id);

        Activity.Current?.AddTag("nexo.correlation_id", id);

        return next(context);
    }
}
