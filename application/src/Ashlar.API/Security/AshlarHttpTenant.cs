namespace Ashlar.API.Security;

/// <summary>
/// Resolves <see cref="AshlarProductOptions.DefaultTenantId"/> vs optional <c>X-Ashlar-Tenant</c> header.
/// The header is client-asserted: trust it only behind built-in auth or an authenticating proxy that sets it.
/// </summary>
public static class AshlarHttpTenant
{
    /// <summary>Default tenant header name (<c>X-Ashlar-Tenant</c>).</summary>
    public const string TenantHeaderName = "X-Ashlar-Tenant";

    /// <summary>Resolves tenant ID from headers and options; sets <paramref name="errorDetail"/> on validation failure.</summary>
    public static bool TryResolve(HttpRequest request, AshlarProductOptions options, out string tenantId, out string? errorDetail)
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
