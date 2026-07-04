using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

/// <summary>Application builder extensions for private license gate middleware.</summary>
public static class PrivateLicenseMiddlewareExtensions
{
    /// <summary>Blocks mutating requests when the private license is invalid or expired.</summary>
    public static IApplicationBuilder UsePrivateLicenseGate(this IApplicationBuilder app) =>
        app.UseMiddleware<PrivateLicenseMiddleware>();
}
