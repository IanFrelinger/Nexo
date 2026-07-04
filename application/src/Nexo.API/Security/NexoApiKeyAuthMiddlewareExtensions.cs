using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Nexo.API.Security;

/// <summary>Application builder extensions for Nexo API key authentication middleware.</summary>
public static class NexoApiKeyAuthMiddlewareExtensions
{
    /// <summary>Enables API key authentication for configured Nexo API routes.</summary>
    public static IApplicationBuilder UseNexoApiKeyAuth(this IApplicationBuilder app)
    {
        return app.UseMiddleware<NexoApiKeyAuthMiddleware>();
    }
}
