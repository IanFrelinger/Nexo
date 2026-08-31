using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>
/// An <see cref="IPeerSource"/> that discovers peers over a Tailscale/WireGuard tailnet — internet-wide
/// peer-to-peer that does NOT depend on a LAN. It reads <c>tailscale status --json</c>, takes every
/// ONLINE peer's tailnet IP, and offers <c>http://&lt;tailnet-ip&gt;:&lt;port&gt;</c> for the pull to try.
/// Like every source it only NOMINATES an address: reaching a peer over the tailnet does not trust it,
/// and its packages face the same Ed25519 seal + trust root + local policy as any other import.
///
/// <para>Drops into the same seam as configured and multicast peers — proof that the network strategy
/// is swappable without touching how packages move or how they are trusted. A Consul, DNS-SD, or
/// rendezvous source would be the same shape: read addresses, hand them over.</para>
///
/// <para>Non-blocking by contract: <see cref="CurrentPeerBaseUrls"/> is called every pull tick and
/// never waits on the subprocess — it returns the last snapshot and kicks off a bounded refresh in the
/// background when the cache is stale. The subprocess is time- and output-bounded; a missing or failing
/// <c>tailscale</c> binary yields an empty peer list, never an exception or a hang.</para>
/// </summary>
public sealed class TailnetPeerSource : IPeerSource
{
    private readonly int _port;
    private readonly TimeSpan _refreshTtl;
    private readonly Func<string?> _statusProvider;
    private readonly Func<DateTimeOffset> _clock;
    private readonly ILogger? _logger;

    private readonly object _lock = new();
    private IReadOnlyList<string> _cached = [];
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;
    private bool _refreshing;

    /// <summary>Production constructor: shells out to the tailscale CLI (path overridable).</summary>
    public TailnetPeerSource(int peerPort, TimeSpan refreshTtl, string tailscaleCommand, ILogger? logger = null)
        : this(peerPort, refreshTtl, () => RunTailscaleStatus(tailscaleCommand, logger), () => DateTimeOffset.UtcNow, logger)
    {
    }

    /// <summary>Test constructor: the status JSON and clock are injected, so no subprocess is spawned.</summary>
    public TailnetPeerSource(int peerPort, TimeSpan refreshTtl, Func<string?> statusProvider, Func<DateTimeOffset> clock, ILogger? logger = null)
    {
        _port = peerPort;
        _refreshTtl = refreshTtl <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : refreshTtl;
        _statusProvider = statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Describe() => "tailnet";

    /// <inheritdoc />
    public IReadOnlyList<string> CurrentPeerBaseUrls()
    {
        MaybeRefresh();
        lock (_lock)
        {
            return _cached;
        }
    }

    private void MaybeRefresh()
    {
        lock (_lock)
        {
            if (_refreshing || _clock() - _cachedAt < _refreshTtl)
            {
                return;
            }
            _refreshing = true;
        }
        // Fire-and-forget: the tick never waits on the tailscale subprocess.
        _ = Task.Run(() =>
        {
            try
            {
                var json = _statusProvider();
                var urls = json is null ? null : ParseTailscalePeers(json, _port);
                lock (_lock)
                {
                    if (urls is not null)
                    {
                        _cached = urls;
                    }
                    _cachedAt = _clock();   // stamp even on failure, so a broken tailscale is not hammered
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Tailnet peer refresh failed");
                lock (_lock) { _cachedAt = _clock(); }
            }
            finally
            {
                lock (_lock) { _refreshing = false; }
            }
        });
    }

    /// <summary>
    /// Parses <c>tailscale status --json</c> into peer base URLs — one per ONLINE peer, using its first
    /// tailnet IP (IPv6 bracketed). Pure and defensive: unknown shapes, missing fields, or invalid JSON
    /// yield an empty list, never a throw. Self is excluded (a node need not pull from itself).
    /// </summary>
    public static IReadOnlyList<string> ParseTailscalePeers(string json, int port)
    {
        var urls = new List<string>();
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return urls;
        }
        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("Peer", out var peers)
                || peers.ValueKind != JsonValueKind.Object)
            {
                return urls;
            }
            foreach (var peer in peers.EnumerateObject())
            {
                var v = peer.Value;
                if (v.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                // Skip peers explicitly offline; treat a missing "Online" as available.
                if (v.TryGetProperty("Online", out var online) && online.ValueKind == JsonValueKind.False)
                {
                    continue;
                }
                if (!v.TryGetProperty("TailscaleIPs", out var ips) || ips.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }
                foreach (var ipElem in ips.EnumerateArray())
                {
                    var ip = ipElem.GetString();
                    if (string.IsNullOrWhiteSpace(ip))
                    {
                        continue;
                    }
                    var host = ip.Contains(':') ? $"[{ip}]" : ip;   // bracket IPv6
                    urls.Add($"http://{host}:{port}");
                    break;   // first IP per peer
                }
            }
        }
        return urls;
    }

    private static string? RunTailscaleStatus(string tailscaleCommand, ILogger? logger)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = tailscaleCommand,
                    Arguments = "status --json",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
            if (!proc.Start())
            {
                return null;
            }
            // Bounded read: cap output so a runaway process cannot balloon memory.
            var stdout = proc.StandardOutput.ReadToEnd();
            if (stdout.Length > 4 * 1024 * 1024)
            {
                stdout = stdout[..(4 * 1024 * 1024)];
            }
            if (!proc.WaitForExit(5000))
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* best effort */ }
                return null;
            }
            return proc.ExitCode == 0 ? stdout : null;
        }
        catch (Exception ex)
        {
            // A missing binary, a permission error, a platform without tailscale — all just mean
            // "no tailnet peers right now", never a crash.
            logger?.LogDebug(ex, "tailscale status could not be run ({Cmd})", tailscaleCommand);
            return null;
        }
    }
}
