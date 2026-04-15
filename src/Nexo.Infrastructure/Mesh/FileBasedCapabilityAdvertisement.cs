using System.Text.Json;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure.Execution.Routing;

namespace Nexo.Infrastructure.Mesh;

/// <summary>
/// File-based capability advertisement. Writes instance info to ~/.nexo/instances.json.
/// </summary>
public sealed class FileBasedCapabilityAdvertisement : ICapabilityAdvertisement
{
    private readonly IInstanceDiscovery _discovery;
    private readonly string _instancesPath;
    private readonly string _peerId;
    private readonly PeerTrustTier _trustTier;

    public FileBasedCapabilityAdvertisement(
        IInstanceDiscovery discovery,
        string? instancesPath = null,
        string? peerId = null,
        string? trustedPeerIdsCsv = null,
        string? untrustedPeerIdsCsv = null,
        PeerTrustTier trustTier = PeerTrustTier.Unknown)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _instancesPath = instancesPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "instances.json");
        _peerId = peerId ?? Guid.NewGuid().ToString("N");
        _trustTier = ResolveLocalTrustTier(_peerId, trustedPeerIdsCsv, untrustedPeerIdsCsv, trustTier);
    }

    /// <inheritdoc />
    public Task AdvertiseAsync(IReadOnlyList<CapabilityDescriptor> capabilities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dir = Path.GetDirectoryName(_instancesPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var entries = new List<PeerEntry>();
        if (File.Exists(_instancesPath))
        {
            try
            {
                var json = File.ReadAllText(_instancesPath);
                var doc = JsonDocument.Parse(json);
                foreach (var entry in doc.RootElement.EnumerateArray())
                {
                    var peerId = entry.TryGetProperty("peerId", out var p) ? p.GetString() ?? "" : "";
                    var endpoint = entry.TryGetProperty("endpoint", out var e) ? e.GetString() ?? "" : "";
                    var caps = new List<string>();
                    if (entry.TryGetProperty("capabilities", out var ca))
                        foreach (var c in ca.EnumerateArray())
                            caps.Add(c.GetString() ?? string.Empty);
                    var trustTier = ParseTrustTier(entry);
                    if (peerId != _peerId)
                        entries.Add(new PeerEntry { PeerId = peerId, Endpoint = endpoint, Capabilities = caps, TrustTier = trustTier });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileBasedCapabilityAdvertisement: failed to parse {_instancesPath}: {ex.Message}");
            }
        }

        entries.Add(new PeerEntry
        {
            PeerId = _peerId,
            Endpoint = $"local:{_peerId}",
            Capabilities = capabilities.Select(c => c.Id).ToList(),
            TrustTier = _trustTier
        });

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        File.WriteAllText(_instancesPath, JsonSerializer.Serialize(entries, serializerOptions));
        return Task.CompletedTask;
    }

    private sealed class PeerEntry
    {
        [System.Text.Json.Serialization.JsonPropertyName("peerId")]
        public string PeerId { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("capabilities")]
        public List<string> Capabilities { get; set; } = new();
        [System.Text.Json.Serialization.JsonPropertyName("trustTier")]
        public PeerTrustTier TrustTier { get; set; } = PeerTrustTier.Unknown;
    }

    private static PeerTrustTier ParseTrustTier(JsonElement entry)
    {
        if (entry.TryGetProperty("trustTier", out var trustNode) &&
            trustNode.ValueKind == JsonValueKind.String &&
            Enum.TryParse<PeerTrustTier>(trustNode.GetString(), true, out var parsed))
        {
            return parsed;
        }

        return PeerTrustTier.Unknown;
    }

    private static PeerTrustTier ResolveLocalTrustTier(
        string peerId,
        string? trustedPeerIdsCsv,
        string? untrustedPeerIdsCsv,
        PeerTrustTier fallback)
    {
        var resolver = new PeerTrustPolicyResolver("any", trustedPeerIdsCsv, untrustedPeerIdsCsv);
        var resolved = resolver.ResolveTier(new PeerInfo
        {
            PeerId = peerId,
            Endpoint = string.Empty,
            Capabilities = Array.Empty<string>(),
            TrustTier = fallback
        });
        return resolved;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PeerInfo>> FindPeersWithCapabilityAsync(string capability, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var all = await _discovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        return all.Where(p => p.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase)).ToList();
    }
}
