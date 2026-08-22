using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Observation;
using Ashlar.Infrastructure.Adaptation;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Adaptation;

/// <summary>Tests for brick decomposer.</summary>
[Trait("Category", "Adaptation")]
public sealed class BrickDecomposerTests
{
    [Fact]
    public async Task DecomposeAsync_ObservationContextBrick_ReturnsManifest()
    {
        var storePath = Path.Combine(Path.GetTempPath(), $"ashlar-adapt-test-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection()
            .AddAdaptationInfrastructure(storePath)
            .BuildServiceProvider();

        var decomposer = services.GetRequiredService<IBrickDecomposer>();
        var brick = new ObservationContextBrick(services.GetRequiredService<IContextAssembler>());

        var manifest = await decomposer.DecomposeAsync(brick);

        Assert.Equal("observation.context", manifest.Id);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal(2, manifest.Interface.Inputs.Count);
        Assert.Single(manifest.Interface.Outputs);
        Assert.NotNull(manifest.ImplementationTypeName);
        Assert.Contains("ObservationContextBrick", manifest.ImplementationTypeName);
    }
}
