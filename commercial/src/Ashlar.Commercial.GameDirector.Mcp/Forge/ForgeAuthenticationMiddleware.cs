using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GameDirector.Mcp.Forge;
/// <summary>
/// When <see cref="ForgeSessionOptions.RequireForgeAuthentication"/> is true, rejects unauthenticated access to <c>/api/forge</c>.
/// </summary>
public sealed class ForgeAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public ForgeAuthenticationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, IOptions<ForgeSessionOptions> forgeOptions)
    {
        if (forgeOptions.Value.RequireForgeAuthentication &&
            ctx.Request.Path.StartsWithSegments("/api/forge", StringComparison.OrdinalIgnoreCase) &&
            ctx.User?.Identity?.IsAuthenticated != true)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(
                "{\"title\":\"Unauthorized\",\"detail\":\"Forge API requires authentication (Ashlar:ForgeSession:RequireForgeAuthentication).\"}");
            return;
        }

        await _next(ctx);
    }
}
