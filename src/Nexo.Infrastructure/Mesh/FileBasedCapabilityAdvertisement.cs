using System.Text.Json;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;

namespace Nexo.Infrastructure.Mesh;

/// <summary>
/// File-based capability advertisement. Writes instance info to ~/.nexo/instances.json.
/// </summary>
public sealed class FileBasedCapabilityAdvertisement : ICapabilityAdvertisement
{
    private readonly IInstanceDiscovery _discovery;
    private readonly string _instancesPath;
    private readonly string _peerId;

    public FileBasedCapabilityAdvertisement(IInstanceDiscovery discovery, string? instancesPath = null, string? peerId = null)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _instancesPath = instancesPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "instances.json");
        _peerId = peerId ?? Guid.NewGuid().ToString("N");
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
                    if (peerId != _peerId)
                        entries.Add(new PeerEntry { PeerId = peerId, Endpoint = endpoint, Capabilities = caps });
                }
            }
            catch
            {
                // Start fresh
            }
        }

        entries.Add(new PeerEntry
        {
            PeerId = _peerId,
            Endpoint = $"local:{_peerId}",
            Capabilities = capabilities.Select(c => c.Id).ToList(),
        });

        File.WriteAllText(_instancesPath, JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
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
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PeerInfo>> FindPeersWithCapabilityAsync(string capability, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var all = _discovery.DiscoverAsync(cancellationToken).GetAwaiter().GetResult();
        var filtered = all.Where(p => p.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<PeerInfo>>(filtered);
    }
}
