using FluentAssertions;
using Moq;
using Ashlar.Core.Application.Mesh.Models;
using Ashlar.Core.Application.Mesh.Ports;
using Ashlar.Infrastructure.Mesh;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Mesh;

/// <summary>Tests for file based capability advertisement.</summary>
public sealed class FileBasedCapabilityAdvertisementTests : IDisposable
{
    private readonly string _tempDir;

    public FileBasedCapabilityAdvertisementTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ashlar-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact(Timeout = 15000)]
    public async Task AdvertiseAsync_WritesToFile()
    {
        var instancesPath = Path.Combine(_tempDir, "instances.json");
        var discovery = new Mock<IInstanceDiscovery>();
        var sut = new FileBasedCapabilityAdvertisement(
            discovery.Object,
            instancesPath: instancesPath,
            peerId: "peer-1");

        var capabilities = new List<CapabilityDescriptor>
        {
            new() { Id = "cap-a", Name = "Capability A" }
        };

        await sut.AdvertiseAsync(capabilities);

        File.Exists(instancesPath).Should().BeTrue();
        var json = File.ReadAllText(instancesPath);
        json.Should().Contain("peer-1");
        json.Should().Contain("cap-a");
    }

    [Fact(Timeout = 15000)]
    public async Task AdvertiseAsync_PreservesExistingPeers()
    {
        var instancesPath = Path.Combine(_tempDir, "instances.json");
        var discovery = new Mock<IInstanceDiscovery>();

        var first = new FileBasedCapabilityAdvertisement(
            discovery.Object,
            instancesPath: instancesPath,
            peerId: "peer-1");
        await first.AdvertiseAsync(new List<CapabilityDescriptor>
        {
            new() { Id = "cap-a", Name = "A" }
        });

        var second = new FileBasedCapabilityAdvertisement(
            discovery.Object,
            instancesPath: instancesPath,
            peerId: "peer-2");
        await second.AdvertiseAsync(new List<CapabilityDescriptor>
        {
            new() { Id = "cap-b", Name = "B" }
        });

        var json = File.ReadAllText(instancesPath);
        json.Should().Contain("peer-1");
        json.Should().Contain("peer-2");
        json.Should().Contain("cap-a");
        json.Should().Contain("cap-b");
    }

    [Fact(Timeout = 15000)]
    public async Task FindPeersWithCapabilityAsync_FiltersCorrectly()
    {
        var discovery = new Mock<IInstanceDiscovery>();
        discovery
            .Setup(d => d.DiscoverAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PeerInfo>
            {
                new()
                {
                    PeerId = "p1",
                    Endpoint = "local:p1",
                    Capabilities = new[] { "cap-a", "cap-b" }
                },
                new()
                {
                    PeerId = "p2",
                    Endpoint = "local:p2",
                    Capabilities = new[] { "cap-c" }
                },
                new()
                {
                    PeerId = "p3",
                    Endpoint = "local:p3",
                    Capabilities = new[] { "cap-a" }
                }
            });

        var instancesPath = Path.Combine(_tempDir, "instances.json");
        var sut = new FileBasedCapabilityAdvertisement(
            discovery.Object,
            instancesPath: instancesPath,
            peerId: "self");

        var peers = await sut.FindPeersWithCapabilityAsync("cap-a");

        peers.Should().HaveCount(2);
        peers.Select(p => p.PeerId).Should().Contain("p1").And.Contain("p3");
        peers.Select(p => p.PeerId).Should().NotContain("p2");
    }
}
