using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Air-gap guard for the local Ollama endpoint. Unless
/// <c>NEXO_ALLOW_REMOTE_MODEL=1</c>, the model base URL must resolve to
/// loopback or an explicit in-cluster allowlist (default: <c>ollama</c>).
/// </summary>
public static class OllamaEndpointPolicy
{
    public const string AllowRemoteEnv = "NEXO_ALLOW_REMOTE_MODEL";
    public const string AllowlistEnv = "NEXO_OLLAMA_ALLOWLIST";

    /// <summary>
    /// Fail fast if <paramref name="baseUrl"/> (or <c>OLLAMA_BASE_URL</c>) would
    /// send proprietary source to an off-box model endpoint.
    /// </summary>
    public static void AssertAirGappedOrThrow(string? baseUrl = null, ILogger? logger = null)
    {
        var resolved = ResolveBaseUrl(baseUrl);
        if (IsRemoteModelExplicitlyAllowed())
        {
            logger?.LogWarning(
                "NEXO_ALLOW_REMOTE_MODEL=1 — allowing non-local Ollama endpoint {Url}. Proprietary source may leave this host.",
                resolved);
            return;
        }

        if (!IsAllowedEndpoint(resolved, out var reason))
        {
            throw new InvalidOperationException(
                "Air-gap violation: Ollama endpoint is not local. " +
                $"Resolved OLLAMA_BASE_URL='{resolved}' ({reason}). " +
                "Use loopback (http://127.0.0.1:11434 / http://localhost:11434), " +
                "add the hostname to NEXO_OLLAMA_ALLOWLIST (comma-separated; default includes 'ollama' for compose), " +
                "or set NEXO_ALLOW_REMOTE_MODEL=1 only if you intentionally accept off-box model traffic.");
        }

        logger?.LogInformation("Ollama air-gap OK — model endpoint {Url} ({Reason})", resolved, reason);
    }

    public static string ResolveBaseUrl(string? baseUrl = null)
    {
        var url = baseUrl
            ?? Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")
            ?? "http://localhost:11434";
        return url.Trim().TrimEnd('/');
    }

    public static bool IsRemoteModelExplicitlyAllowed() =>
        string.Equals(Environment.GetEnvironmentVariable(AllowRemoteEnv), "1", StringComparison.Ordinal);

    public static bool IsAllowedEndpoint(string baseUrl, out string reason)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            reason = "not a valid http(s) absolute URI";
            return false;
        }

        var host = uri.Host.Trim().Trim('[', ']');
        if (string.IsNullOrWhiteSpace(host))
        {
            reason = "empty host";
            return false;
        }

        if (IsLoopbackHost(host))
        {
            reason = "loopback";
            return true;
        }

        foreach (var allowed in GetAllowlist())
        {
            if (string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"allowlist:{allowed}";
                return true;
            }
        }

        // Also accept literal loopback IPs that Uri might normalize oddly.
        if (IPAddress.TryParse(host, out var ip) && IPAddress.IsLoopback(ip))
        {
            reason = "loopback-ip";
            return true;
        }

        reason = $"host '{host}' is neither loopback nor in NEXO_OLLAMA_ALLOWLIST";
        return false;
    }

    public static IReadOnlyList<string> GetAllowlist()
    {
        var raw = Environment.GetEnvironmentVariable(AllowlistEnv);
        var items = new List<string>();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.Length > 0) items.Add(part);
            }
        }

        // In-cluster compose service name used by docker-compose.parser-pipeline.yml
        if (!items.Contains("ollama", StringComparer.OrdinalIgnoreCase))
            items.Add("ollama");
        return items;
    }

    static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("0:0:0:0:0:0:0:1", StringComparison.OrdinalIgnoreCase);
}
