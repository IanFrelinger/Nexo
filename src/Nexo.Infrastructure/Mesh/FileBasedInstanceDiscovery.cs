using System.Text.Json;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;

namespace Nexo.Infrastructure.Mesh;

/// <summary>
/// File-based instance discovery. Reads ~/.nexo/instances.json.
/// </summary>
public sealed class FileBasedInstanceDiscovery : IInstanceDiscovery
{
    private readonly string _instancesPath;

    public FileBasedInstanceDiscovery(string? instancesPath = null)
    {
        _instancesPath = instancesPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "instances.json");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PeerInfo>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var list = new List<PeerInfo>();
        if (!File.Exists(_instancesPath))
            return Task.FromResult<IReadOnlyList<PeerInfo>>(list);

        try
        {
            var json = File.ReadAllText(_instancesPath);
            var doc = JsonDocument.Parse(json);
            foreach (var peer in doc.RootElement.EnumerateArray())
            {
                var peerId = peer.GetProperty("peerId").GetString() ?? "";
                var endpoint = peer.GetProperty("endpoint").GetString() ?? "";
                var caps = new List<string>();
                if (peer.TryGetProperty("capabilities", out var capArr))
                {
                    foreach (var c in capArr.EnumerateArray())
                        caps.Add(c.GetString() ?? "");
                }
                list.Add(new PeerInfo { PeerId = peerId, Endpoint = endpoint, Capabilities = caps });
            }
        }
        catch
        {
            // Return empty on parse error
        }
        return Task.FromResult<IReadOnlyList<PeerInfo>>(list);
    }
}
