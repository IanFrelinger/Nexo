using Microsoft.Extensions.Options;

namespace Ashlar.API.Security;

/// <summary>Application builder extensions for copilot-scoped authorization middleware.</summary>
public static class AshlarCopilotScopedAuthorizationMiddlewareExtensions
{
    /// <summary>Enforces copilot-scoped authorization for configured Ashlar API routes.</summary>
    public static IApplicationBuilder UseAshlarCopilotScopedAuthorization(this IApplicationBuilder app)
        => app.UseMiddleware<AshlarCopilotScopedAuthorizationMiddleware>();
}
