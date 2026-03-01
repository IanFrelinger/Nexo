using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Observation.Ports;
using Xunit;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Observation;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

public sealed class BrickDecomposerTests
{
    [Fact]
    public async Task DecomposeAsync_ObservationContextBrick_ReturnsManifest()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPatternStore>(_ => new Nexo.Infrastructure.Observation.LiteDbPatternStore(Path.Combine(Path.GetTempPath(), "nexo-adapt-test.db")))
            .AddSingleton<IContextAssembler, Nexo.Infrastructure.Observation.ContextAssembler>()
            .AddAdaptationInfrastructure()
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
