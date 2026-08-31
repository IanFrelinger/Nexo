using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// F1+F2, the LAN party: a node SERVES its published signed packages read-only, and a peer PULLS them
/// directly through the same trust-gated import as any other source. These exercise the real
/// HttpListener server and the real HTTP client path on loopback — the wire rules (safe names, size
/// bounds, containment) are pinned on BOTH sides, because a puller must never rely on a server having
/// been polite.
/// </summary>
public sealed class MeshLanPartyTests : IDisposable
{
    private readonly string _dir;
    private readonly string _published;
    private readonly string _project;

    public MeshLanPartyTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-lanparty-" + Guid.NewGuid().ToString("N"));
        _published = Path.Combine(_dir, "published");
        _project = Path.Combine(_dir, "project");
        Directory.CreateDirectory(_published);
        Directory.CreateDirectory(_project);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private async Task<(MeshServeService Service, int Port, HttpClient Client)> StartServeAsync()
    {
        var port = FreePort();
        var service = new MeshServeService(
            new MeshServeSettings(port, _published, "test-node"),
            NullLogger<MeshServeService>.Instance);
        await service.StartAsync(CancellationToken.None);

        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        // Wait for the listener to come up.
        for (var i = 0; i < 50; i++)
        {
            try
            {
                var r = await client.GetAsync($"http://127.0.0.1:{port}/mesh/v1/hello");
                if (r.IsSuccessStatusCode) { return (service, port, client); }
            }
            catch (HttpRequestException) { /* not up yet */ }
            await Task.Delay(100);
        }
        throw new InvalidOperationException("mesh serve did not come up");
    }

    [Fact]
    public async Task Hello_announcesNameVersionAndFingerprintField()
    {
        var (service, port, client) = await StartServeAsync();
        try
        {
            var body = await client.GetStringAsync($"http://127.0.0.1:{port}/mesh/v1/hello");
            body.Should().Contain("test-node").And.Contain(MeshWire.Version).And.Contain("fingerprint");
        }
        finally { await service.StopAsync(CancellationToken.None); client.Dispose(); }
    }

    [Fact]
    public async Task Index_listsPackages_andPkgServesContent_andTraversalIs404()
    {
        await File.WriteAllTextAsync(Path.Combine(_published, "demo-abc123.ashpkg"), "{ \"fake\": true }");
        await File.WriteAllTextAsync(Path.Combine(_published, "._sidecar.ashpkg"), "junk");
        var (service, port, client) = await StartServeAsync();
        try
        {
            var index = await client.GetStringAsync($"http://127.0.0.1:{port}/mesh/v1/index");
            index.Should().Contain("demo-abc123.ashpkg");
            index.Should().NotContain("._sidecar", "unsafe names are not offered");

            var pkg = await client.GetStringAsync($"http://127.0.0.1:{port}/mesh/v1/pkg/demo-abc123.ashpkg");
            pkg.Should().Contain("fake");

            (await client.GetAsync($"http://127.0.0.1:{port}/mesh/v1/pkg/missing.ashpkg")).StatusCode
                .Should().Be(HttpStatusCode.NotFound);
            (await client.GetAsync($"http://127.0.0.1:{port}/mesh/v1/pkg/..%2fsecret.ashpkg")).StatusCode
                .Should().Be(HttpStatusCode.NotFound, "a traversal-shaped name must never resolve");
            (await client.GetAsync($"http://127.0.0.1:{port}/somewhere/else")).StatusCode
                .Should().Be(HttpStatusCode.NotFound);
        }
        finally { await service.StopAsync(CancellationToken.None); client.Dispose(); }
    }

    [Fact]
    public async Task Index_excludesOversizedPackages()
    {
        var big = new string('x', (int)MeshWire.MaxPackageBytes + 1024);
        await File.WriteAllTextAsync(Path.Combine(_published, "huge-000000.ashpkg"), big);
        var (service, port, client) = await StartServeAsync();
        try
        {
            var index = await client.GetStringAsync($"http://127.0.0.1:{port}/mesh/v1/index");
            index.Should().NotContain("huge-000000", "an oversized package is never offered to the LAN");
        }
        finally { await service.StopAsync(CancellationToken.None); client.Dispose(); }
    }

    [Fact]
    public async Task PeerPull_refusesAGarbagePackage_throughTheRealWire()
    {
        await File.WriteAllTextAsync(Path.Combine(_published, "junk-111111.ashpkg"), "{ not a package");
        var (service, port, client) = await StartServeAsync();
        try
        {
            var s = await MeshAutoPullService.PullPeerOnceAsync(client, $"http://127.0.0.1:{port}", _project);

            s.Scanned.Should().Be(1);
            s.Refused.Should().Be(1, "an unopenable package is refused (fail-closed), never applied");
            s.Errors.Should().Be(0);
        }
        finally { await service.StopAsync(CancellationToken.None); client.Dispose(); }
    }

    [Fact]
    public async Task PeerPull_offlinePeer_isAnErrorCount_neverAThrow()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var s = await MeshAutoPullService.PullPeerOnceAsync(client, $"http://127.0.0.1:{FreePort()}", _project);
        s.Errors.Should().Be(1, "a guest who left the party is not an incident");
        s.Scanned.Should().Be(0);
    }

    [Fact]
    public async Task PeerPull_rejectsNonHttpUrls()
    {
        using var client = new HttpClient();
        (await MeshAutoPullService.PullPeerOnceAsync(client, "file:///etc/passwd", _project)).Errors.Should().Be(1);
        (await MeshAutoPullService.PullPeerOnceAsync(client, "not a url", _project)).Errors.Should().Be(1);
    }

    // ---- wire rules, pinned directly -----------------------------------------------------------

    [Theory]
    [InlineData("demo-abc123.ashpkg", true)]
    [InlineData("a.ashpkg", true)]
    [InlineData("../evil.ashpkg", false)]
    [InlineData("..\\evil.ashpkg", false)]
    [InlineData(".hidden.ashpkg", false)]
    [InlineData("._sidecar.ashpkg", false)]
    [InlineData("dir/name.ashpkg", false)]
    [InlineData("name.txt", false)]
    [InlineData("", false)]
    public void SafePackageNames(string name, bool ok) =>
        MeshWire.IsSafePackageName(name).Should().Be(ok);

    [Fact]
    public async Task BoundedRead_returnsNullOverTheBound_andTextUnderIt()
    {
        using var small = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        (await MeshWire.ReadBoundedTextAsync(small, 100)).Should().Be("hello");

        using var big = new MemoryStream(new byte[300]);
        (await MeshWire.ReadBoundedTextAsync(big, 200)).Should().BeNull("over the bound is refusal, not truncation");
    }
}
