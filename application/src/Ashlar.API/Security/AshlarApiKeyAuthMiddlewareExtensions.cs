using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Ashlar.API.Security;

/// <summary>Application builder extensions for Ashlar API key authentication middleware.</summary>
public static class AshlarApiKeyAuthMiddlewareExtensions
{
    /// <summary>Enables API key authentication for configured Ashlar API routes.</summary>
    public static IApplicationBuilder UseAshlarApiKeyAuth(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AshlarApiKeyAuthMiddleware>();
    }
}
