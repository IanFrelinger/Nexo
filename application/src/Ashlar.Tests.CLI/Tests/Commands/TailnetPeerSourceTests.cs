using FluentAssertions;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// TailnetPeerSource: internet-wide P2P through the IPeerSource seam. Pins the pure parse of
/// <c>tailscale status --json</c> (online-only, IPv6 bracketed, self excluded, defensive on junk) and
/// the non-blocking cached-refresh contract (the pull tick never waits on the subprocess).
/// </summary>
public sealed class TailnetPeerSourceTests
{
    private const string SampleStatus = """
    {
      "Self": { "TailscaleIPs": ["100.64.0.1"], "Online": true, "HostName": "me" },
      "Peer": {
        "nodekey:aaa": { "TailscaleIPs": ["100.64.0.2", "fd7a::2"], "Online": true, "HostName": "peer-a" },
        "nodekey:bbb": { "TailscaleIPs": ["fd7a::3"], "Online": true, "HostName": "peer-b" },
        "nodekey:ccc": { "TailscaleIPs": ["100.64.0.4"], "Online": false, "HostName": "offline" }
      }
    }
    """;

    [Fact]
    public void Parse_onlinePeersOnly_firstIp_ipv6Bracketed_selfExcluded()
    {
        var urls = TailnetPeerSource.ParseTailscalePeers(SampleStatus, 7420);

        urls.Should().BeEquivalentTo(new[]
        {
            "http://100.64.0.2:7420",     // peer-a, first (IPv4) IP
            "http://[fd7a::3]:7420",      // peer-b, IPv6 bracketed
        });
        urls.Should().NotContain(u => u.Contains("100.64.0.1"), "self is excluded");
        urls.Should().NotContain(u => u.Contains("100.64.0.4"), "offline peers are excluded");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"Peer": null}""")]
    [InlineData("""{"Peer": []}""")]
    [InlineData("""{"Peer": {"k": {"Online": true}}}""")]   // no TailscaleIPs
    [InlineData("[]")]
    public void Parse_defensiveOnJunk_returnsEmpty_neverThrows(string json)
    {
        TailnetPeerSource.ParseTailscalePeers(json, 7420).Should().BeEmpty();
    }

    [Fact]
    public void CurrentPeerBaseUrls_isNonBlocking_andServesCachedAfterRefresh()
    {
        var now = DateTimeOffset.UtcNow;
        var calls = 0;
        var src = new TailnetPeerSource(
            peerPort: 7420, refreshTtl: TimeSpan.FromSeconds(30),
            statusProvider: () => { Interlocked.Increment(ref calls); return SampleStatus; },
            clock: () => now);

        // First call: cache cold → returns empty immediately (never blocks on the provider).
        src.CurrentPeerBaseUrls().Should().BeEmpty();

        // The refresh runs in the background; wait briefly for it to populate.
        var populated = SpinUntil(() => src.CurrentPeerBaseUrls().Count > 0, TimeSpan.FromSeconds(2));
        populated.Should().BeTrue("the background refresh should populate the cache");
        src.CurrentPeerBaseUrls().Should().Contain("http://100.64.0.2:7420");
    }

    [Fact]
    public void CurrentPeerBaseUrls_withinTtl_doesNotReinvokeTheProvider()
    {
        var now = DateTimeOffset.UtcNow;
        var calls = 0;
        var src = new TailnetPeerSource(
            peerPort: 7420, refreshTtl: TimeSpan.FromSeconds(30),
            statusProvider: () => { Interlocked.Increment(ref calls); return SampleStatus; },
            clock: () => now);   // frozen clock — never past the TTL

        src.CurrentPeerBaseUrls();
        SpinUntil(() => Volatile.Read(ref calls) >= 1, TimeSpan.FromSeconds(2));
        for (var i = 0; i < 5; i++) { src.CurrentPeerBaseUrls(); }

        Volatile.Read(ref calls).Should().Be(1, "within the TTL the subprocess is not re-run every tick");
    }

    private static bool SpinUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) { return true; }
            Thread.Sleep(25);
        }
        return condition();
    }
}
