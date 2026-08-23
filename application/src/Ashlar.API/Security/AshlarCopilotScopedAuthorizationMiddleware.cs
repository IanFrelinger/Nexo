using Microsoft.Extensions.Options;

namespace Ashlar.API.Security;

/// <summary>
/// After API key auth succeeds, restricts <see cref="AshlarApiAuthTier.CopilotScoped"/> credentials to copilot/onboarding/status routes.
/// </summary>
public sealed class AshlarCopilotScopedAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AshlarSecurityOptions _security;

    /// <summary>Creates middleware that enforces copilot-scoped API key route restrictions.</summary>
    public AshlarCopilotScopedAuthorizationMiddleware(RequestDelegate next, IOptions<AshlarSecurityOptions> security)
    {
        _next = next;
        _security = security.Value;
    }

    /// <summary>Restricts copilot-scoped API keys to allowed copilot and onboarding routes.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Items[AshlarAuthContextKeys.AuthTier] is AshlarApiAuthTier tier && tier == AshlarApiAuthTier.CopilotScoped)
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
