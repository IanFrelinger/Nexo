using System.Net;
using System.Net.Sockets;

namespace GameDirector.Mcp.Forge;
/// <summary>
/// Validates URLs for Forge map pipeline HTTP fetches (SSRF mitigation).
/// </summary>
public static class ForgeMapFetchUrlValidator
{
    public static bool TryValidate(string rawUrl, ForgeSessionOptions options, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            error = "URL is required.";
            return false;
        }

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri))
        {
            error = "URL must be an absolute URI.";
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            error = "Only http and https URLs are allowed.";
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttp && !options.AllowInsecureMapFetch)
        {
            error = "http URLs are disabled; use https or set Ashlar:ForgeSession:AllowInsecureMapFetch.";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            error = "URLs with user info are not allowed.";
            return false;
        }

        var host = uri.IdnHost;
        if (string.IsNullOrWhiteSpace(host))
        {
            error = "Host is required.";
            return false;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            error = "localhost is not allowed.";
            return false;
        }

        var allowed = options.AllowedMapFetchHosts ?? [];
        if (allowed.Count == 0 && !options.AllowMapFetchWhenAllowedHostsEmpty)
        {
            error = "No allowed map fetch hosts configured (Ashlar:ForgeSession:AllowedMapFetchHosts).";
            return false;
        }

        if (allowed.Count > 0 && !HostMatchesAllowlist(host, allowed))
        {
            error = $"Host '{host}' is not in Ashlar:ForgeSession:AllowedMapFetchHosts.";
            return false;
        }

        if (IPAddress.TryParse(host, out var literal))
        {
            if (IsBlockedAddress(literal))
            {
                error = "IP literal resolves to a loopback, link-local, or private address.";
                return false;
            }
        }
        else
        {
            IPAddress[] addresses;
            try
            {
                addresses = Dns.GetHostAddresses(host);
            }
            catch
            {
                error = "DNS resolution failed for host.";
                return false;
            }

            if (addresses.Length == 0)
            {
                error = "Host could not be resolved.";
                return false;
            }

            foreach (var a in addresses)
            {
                if (IsBlockedAddress(a))
                {
                    error = "Host resolves to a loopback, link-local, or private address.";
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>True if the address must not be used for outbound map fetches (loopback, RFC1918, etc.).</summary>
    public static bool IsBlockedIp(IPAddress ip) => IsBlockedAddress(ip);

    private static bool HostMatchesAllowlist(string host, IReadOnlyList<string> allowed)
    {
        foreach (var entry in allowed)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;
            var e = entry.Trim();
            if (e.StartsWith("*.", StringComparison.Ordinal))
            {
                var suffix = e[2..];
                if (host.Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (host.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (host.Equals(e, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBlockedAddress(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            if (bytes[0] == 127) return true;
            if (bytes[0] == 0) return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv4MappedToIPv6)
                return IsBlockedAddress(ip.MapToIPv4());
            if (ip.IsIPv6LinkLocal)
                return true;
            var s = ip.ToString();
            if (s.StartsWith("fc", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("fd", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
