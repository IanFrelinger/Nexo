using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Mesh;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 9 dogfood gate: instance mesh discover and advertise for Nexo.
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
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-dogfood-block9");
        _instancesPath = Path.Combine(_tempDir, "instances.json");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 15000)]
    public async Task InstanceMesh_AdvertiseAndDiscover_ReturnsNexoPeer()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddMeshInfrastructure(_instancesPath)
            .BuildServiceProvider();

        var adv = services.GetRequiredService<ICapabilityAdvertisement>();
        var discovery = services.GetRequiredService<IInstanceDiscovery>();

        await adv.AdvertiseAsync(new[]
        {
            new CapabilityDescriptor { Id = "nexo-cli", Name = "Nexo CLI" },
            new CapabilityDescriptor { Id = "nexo-dogfood", Name = "Nexo Dogfood" },
        });

        var peers = await discovery.DiscoverAsync();
        Assert.NotEmpty(peers);
        Assert.Contains(peers, p => p.Capabilities.Contains("nexo-cli", StringComparer.OrdinalIgnoreCase));

        var withCap = await adv.FindPeersWithCapabilityAsync("nexo-dogfood");
        Assert.NotEmpty(withCap);
    }
}
