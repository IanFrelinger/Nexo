/// <summary>
/// Shared parser for <c>ASHLAR_DEPLOYMENT_PROFILE</c>. Linked into hosting and
/// protocol assemblies so AirGapped / SecureWorkstation aliases cannot drift.
/// </summary>
internal static class AshlarDeploymentProfileEnvironment
{
    internal static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        normalized = raw.Trim().ToLowerInvariant();
        return true;
    }

    internal static bool IsAirGapped(string? raw) =>
        TryNormalize(raw, out var normalized) && normalized is "airgapped" or "air-gapped";

    internal static bool IsSecureWorkstation(string? raw) =>
        TryNormalize(raw, out var normalized) &&
        normalized is "secureworkstation" or "secure-workstation" or "workstation";

    /// <summary>
    /// Profiles that must not dial remote MCP/A2A or expose an A2A server.
    /// Local MCP server (IDE stdio) stays allowed on SecureWorkstation.
    /// </summary>
    internal static bool ForbidsRemoteProtocolEgress(string? raw) =>
        IsAirGapped(raw) || IsSecureWorkstation(raw);

    internal static bool TryParseKnown(string? raw, out string canonical)
    {
        canonical = "full";
        if (!TryNormalize(raw, out var normalized))
        {
            return false;
        }

        canonical = normalized switch
        {
            "full" => "full",
            "server" => "server",
            "edge" => "edge",
            "airgapped" or "air-gapped" => "air-gapped",
            "system" or "core" => "system",
            "secureworkstation" or "secure-workstation" or "workstation" => "secure-workstation",
            _ => normalized
        };

        return normalized is "full" or "server" or "edge" or "airgapped" or "air-gapped"
            or "system" or "core"
            or "secureworkstation" or "secure-workstation" or "workstation";
    }

    internal static string DisplayName(string? raw)
    {
        if (IsSecureWorkstation(raw))
        {
            return "SecureWorkstation";
        }

        if (IsAirGapped(raw))
        {
            return "AirGapped";
        }

        return raw ?? string.Empty;
    }
}
