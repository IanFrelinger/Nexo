using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Observation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

public sealed class BrickRecompilerTests
{
    [Fact]
    public async Task RecompileAsync_ObservationContextManifest_ReturnsBrick()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"nexo-recompile-{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection()
                .AddSingleton<IPatternStore>(_ => new LiteDbPatternStore(dbPath))
                .AddSingleton<IContextAssembler, ContextAssembler>()
                .AddAdaptationInfrastructure()
                .BuildServiceProvider();

            var recompiler = services.GetRequiredService<IBrickRecompiler>();
            var manifest = new BrickManifest
            {
                Id = "observation.context",
                Name = "Observation Context",
                Version = "1.0.0",
                Interface = new BrickInterface { Inputs = [], Outputs = [] },
                ImplementationTypeName = typeof(ObservationContextBrick).AssemblyQualifiedName,
            };

            var brick = await recompiler.RecompileAsync(manifest);

            brick.Should().NotBeNull();
            brick!.Id.Should().Be("observation.context");
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task RecompileAsync_UnknownType_ReturnsNull()
    {
        var services = new ServiceCollection()
            .AddAdaptationInfrastructure()
            .BuildServiceProvider();

        var recompiler = services.GetRequiredService<IBrickRecompiler>();
        var manifest = new BrickManifest
        {
            Id = "fake.brick",
            Name = "Fake",
            Version = "1.0.0",
            Interface = new BrickInterface { Inputs = [], Outputs = [] },
            ImplementationTypeName = "NonExistent.TypeName, NonExistent.Assembly",
        };

        var brick = await recompiler.RecompileAsync(manifest);

        brick.Should().BeNull();
    }
}
