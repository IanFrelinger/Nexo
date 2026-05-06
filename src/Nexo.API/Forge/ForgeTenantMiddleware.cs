using Microsoft.AspNetCore.Http;

namespace Nexo.API.Forge;

/// <summary>
/// Reads optional tenant header and stores normalized tenant id on <see cref="HttpContext.Items"/>.
/// </summary>
public sealed class ForgeTenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _headerName;

    public ForgeTenantMiddleware(RequestDelegate next, Microsoft.Extensions.Options.IOptions<ForgeSessionOptions> opts)
    {
        _next = next;
        _headerName = string.IsNullOrWhiteSpace(opts.Value.TenantHeaderName)
            ? "X-Forge-Tenant"
            : opts.Value.TenantHeaderName.Trim();
    }

    public Task InvokeAsync(HttpContext ctx)
    {
        var raw = ctx.Request.Headers[_headerName].FirstOrDefault();
        var tenant = ForgeTenantNormalizer.Normalize(raw);
        ctx.Items[ForgeTenantResolver.HttpContextItemKey] = tenant;
        return _next(ctx);
    }
}
