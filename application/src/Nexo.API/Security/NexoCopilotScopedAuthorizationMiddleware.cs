using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

/// <summary>
/// After API key auth succeeds, restricts <see cref="NexoApiAuthTier.CopilotScoped"/> credentials to copilot/onboarding/status routes.
/// </summary>
public sealed class NexoCopilotScopedAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly NexoSecurityOptions _security;

    public NexoCopilotScopedAuthorizationMiddleware(RequestDelegate next, IOptions<NexoSecurityOptions> security)
    {
        _next = next;
        _security = security.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Items[NexoAuthContextKeys.AuthTier] is NexoApiAuthTier tier && tier == NexoApiAuthTier.CopilotScoped)
        {
            if (!IsCopilotScopedAllowed(context.Request.Path, context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"title\":\"Forbidden\",\"detail\":\"This API key is scoped to copilot and onboarding endpoints only.\"}");
                return;
            }
        }

        await _next(context);
    }

    private bool IsCopilotScopedAllowed(PathString path, string method)
    {
        var pathValue = path.Value ?? string.Empty;

        if (HttpMethods.IsPost(method) &&
            string.Equals(pathValue, "/api/copilot/task", StringComparison.OrdinalIgnoreCase))
            return true;

        if (HttpMethods.IsGet(method) &&
            pathValue.StartsWith("/api/copilot/tasks", StringComparison.OrdinalIgnoreCase))
            return true;

        if (HttpMethods.IsGet(method) &&
            string.Equals(pathValue, "/api/onboarding/status", StringComparison.OrdinalIgnoreCase))
            return true;

        if (HttpMethods.IsGet(method) &&
            string.Equals(pathValue, "/api/status", StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var prefix in _security.ExcludedAuthorizationPaths ?? [])
        {
            if (string.IsNullOrWhiteSpace(prefix))
                continue;
            var trimmed = prefix.Trim();
            if (pathValue.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

public static class NexoCopilotScopedAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseNexoCopilotScopedAuthorization(this IApplicationBuilder app)
        => app.UseMiddleware<NexoCopilotScopedAuthorizationMiddleware>();
}
