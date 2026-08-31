using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// F3, zero-config presence: the multicast beacon, the bounded self-expiring peer registry, and the
/// IPeerSource strategy seam that lets discovery (or any future source — tailnet, rendezvous) feed
/// the same trust-gated pull. Beacon datagrams are UNTRUSTED network input, so parsing is pinned
/// against malformed, oversized, mis-versioned and invalid-field packets. The live multicast
/// round-trip is exercised best-effort: environments that do not deliver multicast (some CI runners)
/// pass vacuously; the logic above the socket is pinned strictly either way.
/// </summary>
[Xunit.Collection("MeshIntegration")]
public sealed class MeshDiscoveryTests : IDisposable
{
    private readonly string _dir;

    public MeshDiscoveryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-disc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private const string FpA = "ed25519:aaaa1111bbbb2222";
    private const string FpB = "ed25519:cccc3333dddd4444";

    // ---- the beacon: untrusted input -----------------------------------------------------------

    [Fact]
    public void Beacon_roundTrips()
    {
        var bytes = MeshBeacon.Encode("study-node", FpA, 7420);
        MeshBeacon.TryParse(bytes, out var p).Should().BeTrue();
        (p.Name, p.Fp, p.Port).Should().Be(("study-node", FpA, 7420));
    }

    [Theory]
    [InlineData("""{"v":"mesh/v0","name":"x","fp":"ed25519:aaaa1111bbbb2222","port":7420}""")]  // wrong version
    [InlineData("""{"v":"mesh/v1","name":"x","fp":"not-a-fingerprint","port":7420}""")]          // bad fp
    [InlineData("""{"v":"mesh/v1","name":"x","fp":"ed25519:aaaa1111bbbb2222","port":0}""")]      // bad port
    [InlineData("""{"v":"mesh/v1","name":"","fp":"ed25519:aaaa1111bbbb2222","port":7420}""")]    // empty name
    [InlineData("not json at all")]
    [InlineData("")]
    public void Beacon_rejectsHostileOrMalformedDatagrams(string wire)
    {
        MeshBeacon.TryParse(System.Text.Encoding.UTF8.GetBytes(wire), out _)
            .Should().BeFalse("a hostile beacon is simply not heard");
    }

    [Fact]
    public void Beacon_rejectsOversizedDatagrams()
    {
        var big = System.Text.Encoding.UTF8.GetBytes(
            $$"""{"v":"mesh/v1","name":"{{new string('x', 600)}}","fp":"{{FpA}}","port":7420}""");
        MeshBeacon.TryParse(big, out _).Should().BeFalse("oversized datagrams are ignored unparsed");
    }

    // ---- the registry: bounded and self-expiring -----------------------------------------------

    [Fact]
    public void Registry_reportsRefreshesAndExpires()
    {
        var reg = new MeshDiscoveryRegistry();
        var now = DateTimeOffset.UtcNow;
        reg.Report(new DiscoveredPeer("a", FpA, "10.0.0.1", 7420, now - MeshBeacon.Ttl - TimeSpan.FromSeconds(5)));
        reg.Report(new DiscoveredPeer("b", FpB, "10.0.0.2", 7420, now));

        var live = reg.Snapshot(now);

        live.Should().ContainSingle(p => p.Name == "b", "a peer silent past the TTL has left the party");
    }

    [Fact]
    public void Registry_isBounded_evictingTheStalest()
    {
        var reg = new MeshDiscoveryRegistry();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < MeshDiscoveryRegistry.MaxPeers + 1; i++)
        {
            reg.Report(new DiscoveredPeer($"p{i}", FpA, $"10.0.{i / 250}.{i % 250}", 7420, now.AddSeconds(i)));
        }

        var live = reg.Snapshot(now.AddSeconds(MeshDiscoveryRegistry.MaxPeers));

        live.Count.Should().Be(MeshDiscoveryRegistry.MaxPeers, "a beacon-spammer cannot grow the table unbounded");
        live.Should().NotContain(p => p.Name == "p0", "the stalest entry is the one evicted");
    }

    [Fact]
    public void Beacon_rejectsControlCharsInName()
    {
        var wire = System.Text.Encoding.UTF8.GetBytes(
            "{\"v\":\"mesh/v1\",\"name\":\"node\\u001b[31mred\",\"fp\":\"" + FpA + "\",\"port\":7420}");
        MeshBeacon.TryParse(wire, out _).Should().BeFalse("a name with an escape sequence must never be admitted");
    }

    [Fact]
    public void Registry_perAddressCap_containsOneHostileSource_neverEvictsOtherAddresses()
    {
        var reg = new MeshDiscoveryRegistry();
        var now = DateTimeOffset.UtcNow;
        // An honest peer at its own address.
        reg.Report(new DiscoveredPeer("honest", FpB, "10.0.0.9", 7420, now));
        // One hostile source floods varied ports (same IP); millisecond spacing keeps them all within
        // the TTL so the test isolates EVICTION behaviour from expiry.
        for (var port = 1; port <= 200; port++)
        {
            reg.Report(new DiscoveredPeer("evil", FpA, "10.0.0.1", port, now.AddMilliseconds(port)));
        }

        var live = reg.Snapshot(now.AddSeconds(1));

        live.Should().Contain(p => p.Address == "10.0.0.9", "an honest peer at another address is never evicted by a flood");
        live.Count(p => p.Address == "10.0.0.1").Should().Be(MeshDiscoveryRegistry.MaxPerAddress,
            "one source is capped to its own quota of slots");
    }

    // ---- the strategy seam ---------------------------------------------------------------------

    [Fact]
    public void MergePeerUrls_unionsConfiguredAndSources_dedupes_andCaps()
    {
        var reg = new MeshDiscoveryRegistry();
        reg.Report(new DiscoveredPeer("a", FpA, "10.0.0.1", 7420, DateTimeOffset.UtcNow));
        var sources = new IPeerSource[]
        {
            new ConfiguredPeerSource(["http://10.0.0.1:7420", "http://static:7420"]),   // dup of discovered
            new MulticastPeerSource(reg),
            new ThrowingSource(),                                                       // never blocks the rest
        };

        var urls = MeshAutoPullService.MergePeerUrls(["HTTP://STATIC:7420"], sources);

        urls.Should().BeEquivalentTo(["HTTP://STATIC:7420", "http://10.0.0.1:7420"],
            o => o.WithoutStrictOrdering());
    }

    [Fact]
    public void MergePeerUrls_capsThePerTickDialOut()
    {
        var many = Enumerable.Range(0, 50).Select(i => $"http://10.1.0.{i}:7420").ToList();
        MeshAutoPullService.MergePeerUrls(many, null).Count
            .Should().Be(MeshAutoPullService.MaxPeersPerTick, "one tick never dials out unbounded");
    }

    private sealed class ThrowingSource : IPeerSource
    {
        public string Describe() => "broken";
        public IReadOnlyList<string> CurrentPeerBaseUrls() => throw new InvalidOperationException("boom");
    }

    // ---- live multicast round-trip (best-effort — vacuous where the env drops multicast) --------

    [Fact]
    public async Task Discovery_liveLoopback_announcerIsHeard_andSelfIsFiltered()
    {
        var port = Random.Shared.Next(21000, 39000);
        var regA = new MeshDiscoveryRegistry();
        var regB = new MeshDiscoveryRegistry();
        var a = new MeshDiscoveryService(
            new MeshDiscoverySettings("node-a", FpA, ServePort: 17001, Path.Combine(_dir, "a"), port),
            regA, NullLogger<MeshDiscoveryService>.Instance);
        var b = new MeshDiscoveryService(
            new MeshDiscoverySettings("node-b", FpB, ServePort: null, Path.Combine(_dir, "b"), port),
            regB, NullLogger<MeshDiscoveryService>.Instance);

        await b.StartAsync(CancellationToken.None);
        await a.StartAsync(CancellationToken.None);
        try
        {
            for (var i = 0; i < 80 && regB.Snapshot(DateTimeOffset.UtcNow).Count == 0; i++)
            {
                await Task.Delay(100);
            }
            var heard = regB.Snapshot(DateTimeOffset.UtcNow);
            if (heard.Count == 0)
            {
                return;   // this environment does not deliver multicast — logic is pinned above
            }
            heard.Should().ContainSingle(p => p.Fingerprint == FpA && p.Port == 17001 && p.Name == "node-a");
            regA.Snapshot(DateTimeOffset.UtcNow)
                .Should().NotContain(p => p.Fingerprint == FpA, "a node must filter its own echo");
        }
        finally
        {
            await a.StopAsync(CancellationToken.None);
            await b.StopAsync(CancellationToken.None);
        }
    }
}
