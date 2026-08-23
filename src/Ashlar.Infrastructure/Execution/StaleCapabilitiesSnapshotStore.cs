using Ashlar.Brick.Contracts.Capabilities;

namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// Best-effort in-memory cache for last known remote node capabilities.
/// Used to keep capability hints available when /api/capabilities is temporarily unreachable.
/// </summary>
public sealed class StaleCapabilitiesSnapshotStore
{
    private readonly Dictionary<string, NodeCapabilityManifestDto> _byBaseUrl = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    /// <summary>Gets .</summary>
    public NodeCapabilityManifestDto? Get(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var normalized = Normalize(baseUrl);
        lock (_gate)
        {
            return _byBaseUrl.TryGetValue(normalized, out var value)
                ? value
                : null;
        }
    }

    /// <summary>Put.</summary>
    public void Put(string baseUrl, NodeCapabilityManifestDto value)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || value is null)
        {
            return;
        }

        var normalized = Normalize(baseUrl);
        lock (_gate)
        {
            _byBaseUrl[normalized] = value;
        }
    }

    private static string Normalize(string baseUrl) => baseUrl.Trim().TrimEnd('/');
}
