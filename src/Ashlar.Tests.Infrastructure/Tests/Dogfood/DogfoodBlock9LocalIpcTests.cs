using System.Text;
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
/// Phase E: Instance Mesh Dogfood (Block 9). Validates two Ashlar instances on the same machine
/// share a capability via local IPC. One instance fulfills, the other requests and receives the artifact.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
public sealed class DogfoodBlock9LocalIpcTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _instancesPath;
    private readonly IDisposable _tempDirCleanup;

    public DogfoodBlock9LocalIpcTests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-dogfood-block9-ipc");
        _instancesPath = Path.Combine(_tempDir, "instances.json");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 15000)]
    public async Task TwoInstances_LocalIpc_RequestFulfilledAndArtifactValidated()
    {
        const string capabilityId = "ashlar-test-helper";
        const string expectedContent = "artifact-from-fulfiller";

        var servicesFulfiller = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddMeshInfrastructure(_instancesPath, "peer-fulfiller")
            .BuildServiceProvider();

        var servicesRequester = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddMeshInfrastructure(_instancesPath, "peer-requester")
            .BuildServiceProvider();

        var adv = servicesFulfiller.GetRequiredService<ICapabilityAdvertisement>();
        var fulfiller = servicesFulfiller.GetRequiredService<ICapabilityFulfiller>();
        var requester = servicesRequester.GetRequiredService<ICapabilityRequester>();

        await adv.AdvertiseAsync(new[]
        {
            new CapabilityDescriptor { Id = capabilityId, Name = "Ashlar Test Helper" },
        });

        fulfiller.RegisterHandler(capabilityId, (format, ct) =>
            Task.FromResult(new Artifact
            {
                Format = format,
                Content = Encoding.UTF8.GetBytes(expectedContent),
            }));

        var fulfillerTask = Task.Run(async () =>
        {
            for (var i = 0; i < 100; i++)
            {
                if (await fulfiller.ProcessOneAsync())
                    break;
                await Task.Delay(50);
            }
        });

        var artifact = await requester.RequestAsync(capabilityId, ArtifactFormat.Binary);
        await fulfillerTask;

        Assert.NotNull(artifact);
        Assert.Equal(expectedContent, Encoding.UTF8.GetString(artifact.Content));
        Assert.Equal(ArtifactFormat.Binary, artifact.Format);
    }
}
