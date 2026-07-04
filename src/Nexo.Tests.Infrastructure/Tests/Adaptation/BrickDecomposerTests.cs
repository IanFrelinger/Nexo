using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.Adaptation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>Tests for brick decomposer.</summary>
[Trait("Category", "Adaptation")]
public sealed class BrickDecomposerTests
{
    [Fact]
    public async Task DecomposeAsync_ObservationContextBrick_ReturnsManifest()
    {
        var storePath = Path.Combine(Path.GetTempPath(), $"nexo-adapt-test-{Guid.NewGuid():N}.db");
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
