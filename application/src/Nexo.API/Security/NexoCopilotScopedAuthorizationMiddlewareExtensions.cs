using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

/// <summary>Application builder extensions for copilot-scoped authorization middleware.</summary>
public static class NexoCopilotScopedAuthorizationMiddlewareExtensions
{
    /// <summary>Enforces copilot-scoped authorization for configured Nexo API routes.</summary>
    public static IApplicationBuilder UseNexoCopilotScopedAuthorization(this IApplicationBuilder app)
        => app.UseMiddleware<NexoCopilotScopedAuthorizationMiddleware>();
}
