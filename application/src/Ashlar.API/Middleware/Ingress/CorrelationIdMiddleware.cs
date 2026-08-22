using System.Diagnostics;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>
/// Accepts or generates <c>X-Correlation-Id</c>, exposes it on <see cref="HttpContext.Items"/> and response headers,
/// and tags the current <see cref="Activity"/> when present.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>Request and response header name for the correlation identifier.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>HttpContext item key storing the resolved correlation identifier.</summary>
    public const string HttpContextItemKey = "Ashlar.CorrelationId";

    /// <summary>Accepts or generates a correlation ID and echoes it on the response.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var id = string.IsNullOrWhiteSpace(incoming) ? Guid.NewGuid().ToString("N") : incoming.Trim();

        context.Items[HttpContextItemKey] = id;
        context.Response.Headers.Append(HeaderName, id);

        Activity.Current?.AddTag("ashlar.correlation_id", id);

        return next(context);
    }
}
