namespace Nexo.API.Security;

/// <summary>
/// Resolves <see cref="NexoProductOptions.DefaultTenantId"/> vs optional <c>X-Nexo-Tenant</c> header.
/// </summary>
public static class NexoHttpTenant
{
    public const string TenantHeaderName = "X-Nexo-Tenant";

    public static bool TryResolve(HttpRequest request, NexoProductOptions options, out string tenantId, out string? errorDetail)
    {
        tenantId = string.IsNullOrWhiteSpace(options.DefaultTenantId) ? "default" : options.DefaultTenantId.Trim();
        errorDetail = null;

        var raw = request.Headers[TenantHeaderName].ToString().Trim();
        if (string.IsNullOrEmpty(raw))
            return true;

        if (raw.Length > 128)
        {
            errorDetail = $"Tenant header '{TenantHeaderName}' exceeds maximum length (128).";
            return false;
        }

        var normalizedAllow = (options.AllowedTenantIds ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.Ordinal);

        if (normalizedAllow.Count > 0 && !normalizedAllow.Contains(raw))
        {
            errorDetail = $"Tenant '{raw}' is not in the configured allow-list.";
            return false;
        }

        tenantId = raw;
        return true;
    }
}
