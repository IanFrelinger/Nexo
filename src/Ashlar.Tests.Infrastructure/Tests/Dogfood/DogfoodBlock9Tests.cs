using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Mesh.Models;
using Ashlar.Core.Application.Mesh.Ports;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Mesh;
using Ashlar.Tests.Application.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 9 dogfood gate: instance mesh discover and advertise for Ashlar.
/// Validates IInstanceDiscovery and ICapabilityAdvertisement with temp instances.json.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
public sealed class DogfoodBlock9Tests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _instancesPath;
    private readonly IDisposable _tempDirCleanup;

    public DogfoodBlock9Tests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-dogfood-block9");
        _instancesPath = Path.Combine(_tempDir, "instances.json");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 15000)]
    public async Task InstanceMesh_AdvertiseAndDiscover_ReturnsAshlarPeer()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddMeshInfrastructure(_instancesPath)
            .BuildServiceProvider();

        var adv = services.GetRequiredService<ICapabilityAdvertisement>();
        var discovery = services.GetRequiredService<IInstanceDiscovery>();

        await adv.AdvertiseAsync(new[]
        {
            new CapabilityDescriptor { Id = "ashlar-cli", Name = "Ashlar CLI" },
            new CapabilityDescriptor { Id = "ashlar-dogfood", Name = "Ashlar Dogfood" },
        });

        var peers = await discovery.DiscoverAsync();
        Assert.NotEmpty(peers);
        Assert.Contains(peers, p => p.Capabilities.Contains("ashlar-cli", StringComparer.OrdinalIgnoreCase));

        var withCap = await adv.FindPeersWithCapabilityAsync("ashlar-dogfood");
        Assert.NotEmpty(withCap);
    }
}
