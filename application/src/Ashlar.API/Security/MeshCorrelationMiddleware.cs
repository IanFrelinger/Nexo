using Microsoft.AspNetCore.Http;

namespace Ashlar.API.Security;

/// <summary>
/// Ensures every mesh-related request has a <c>X-Ashlar-Correlation-Id</c> (from client or generated) and echoes it on the response.
/// </summary>
public sealed class MeshCorrelationMiddleware
{
    /// <summary>HttpContext item key storing the resolved correlation identifier.</summary>
    public const string HttpContextItemKey = "Ashlar.Mesh.CorrelationId";

    /// <summary>Request and response header name for the correlation identifier.</summary>
    public const string HeaderName = "X-Ashlar-Correlation-Id";

    private readonly RequestDelegate _next;

    /// <summary>Creates middleware that propagates mesh correlation IDs.</summary>
    public MeshCorrelationMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>Assigns or echoes a correlation ID for mesh and brick execute requests.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        var isMesh = path.StartsWithSegments("/api/mesh", StringComparison.OrdinalIgnoreCase);
        var isBrickExecute = HttpMethods.IsPost(context.Request.Method) &&
                             path.StartsWithSegments("/api/bricks", StringComparison.OrdinalIgnoreCase) &&
                             path.Value?.Contains("/execute", StringComparison.OrdinalIgnoreCase) == true;

        if (!isMesh && !isBrickExecute)
        {
            await _next(context);
            return;
        }

        var incoming = context.Request.Headers[HeaderName].ToString().Trim();
        var correlationId = string.IsNullOrWhiteSpace(incoming) ? Guid.NewGuid().ToString("N") : incoming;
        context.Items[HttpContextItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderName))
                context.Response.Headers.Append(HeaderName, correlationId);
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
