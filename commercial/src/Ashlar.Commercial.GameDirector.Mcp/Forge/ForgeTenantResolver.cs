using Microsoft.AspNetCore.Http;

namespace GameDirector.Mcp.Forge;
/// <summary>
/// Resolves the current Forge tenant id from <see cref="HttpContext.Items"/> (set by <see cref="ForgeTenantMiddleware"/>).
/// </summary>
internal static class ForgeTenantResolver
{
    public const string HttpContextItemKey = "Ashlar.Forge.TenantId";

    public static string CurrentTenantId(IHttpContextAccessor? http)
    {
        if (http?.HttpContext?.Items.TryGetValue(HttpContextItemKey, out var v) == true &&
            v is string s &&
            !string.IsNullOrWhiteSpace(s))
            return s;
        return ForgeTenantNormalizer.DefaultTenantId;
    }
}
