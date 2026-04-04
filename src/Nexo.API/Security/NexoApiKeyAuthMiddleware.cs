using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Nexo.API.Security;

/// <summary>
/// Optional API key middleware for mutating Nexo API routes.
/// When enabled, requests to configured paths must include a matching key.
/// </summary>
public sealed class NexoApiKeyAuthMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly NexoSecurityOptions _options;

    public NexoApiKeyAuthMiddleware(RequestDelegate next, IOptions<NexoSecurityOptions> optionsAccessor)
    {
        _next = next;
        _options = optionsAccessor.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldProtect(context.Request))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            await _next(context);
            return;
        }

        var configured = _options.ApiKey.Trim();
        var provided = context.Request.Headers[_options.ApiKeyHeaderName].ToString().Trim();
        if (!string.Equals(configured, provided, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var payload = JsonSerializer.Serialize(new
            {
                title = "Unauthorized",
                detail = $"Missing or invalid API key header '{_options.ApiKeyHeaderName}'."
            }, JsonOptions);
            await context.Response.WriteAsync(payload);
            return;
        }

        await _next(context);
    }

    private bool ShouldProtect(HttpRequest request)
    {
        if (!_options.RequireApiKeyForMutatingEndpoints)
            return false;

        var method = request.Method;
        if (!HttpMethods.IsPost(method) &&
            !HttpMethods.IsPut(method) &&
            !HttpMethods.IsPatch(method) &&
            !HttpMethods.IsDelete(method))
        {
            return false;
        }

        return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
               && !_options.ExcludedApiKeyPaths.Any(prefix =>
                   request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

public static class NexoApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseNexoApiKeyAuth(this IApplicationBuilder app)
    {
        return app.UseMiddleware<NexoApiKeyAuthMiddleware>();
    }
}
