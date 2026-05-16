using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Nexo.API.Forge;

/// <summary>
/// Resolves Forge tenant id from claims (optional) or a header, and stores it on <see cref="HttpContext.Items"/>.
/// </summary>
public sealed class ForgeTenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _headerName;
    private readonly IOptions<ForgeSessionOptions> _forgeOptions;

    public ForgeTenantMiddleware(RequestDelegate next, IOptions<ForgeSessionOptions> opts)
    {
        _next = next;
        _forgeOptions = opts;
        _headerName = string.IsNullOrWhiteSpace(opts.Value.TenantHeaderName)
            ? "X-Forge-Tenant"
            : opts.Value.TenantHeaderName.Trim();
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var forgeOpts = _forgeOptions.Value;
        string? raw;

        if (forgeOpts.BindTenantFromClaims &&
            !string.IsNullOrWhiteSpace(forgeOpts.TenantClaimType))
        {
            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                raw = ctx.User.FindFirstValue(forgeOpts.TenantClaimType);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync(
                        "{\"title\":\"Forbidden\",\"detail\":\"Authenticated Forge requests require the configured tenant claim.\"}");
                    return;
                }
            }
            else if (!forgeOpts.AllowTenantHeaderWhenClaimsBindingEnabled)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(
                    "{\"title\":\"Unauthorized\",\"detail\":\"Forge tenant binding is configured for authenticated callers only.\"}");
                return;
            }
            else
            {
                raw = ctx.Request.Headers[_headerName].FirstOrDefault();
            }
        }
        else
        {
            raw = ctx.Request.Headers[_headerName].FirstOrDefault();
        }

        var tenant = ForgeTenantNormalizer.Normalize(raw);
        ctx.Items[ForgeTenantResolver.HttpContextItemKey] = tenant;
        await _next(ctx);
    }
}
