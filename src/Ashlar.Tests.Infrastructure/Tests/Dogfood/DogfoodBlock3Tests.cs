using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Observation;
using Ashlar.Tests.Application.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 3 dogfood gate: adaptation engine has successfully improved at least one of its own bricks.
/// Validates decompose + recompile of ObservationContextBrick (a Ashlar brick from Block 1).
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
[Trait("Category", "Adaptation")]
public sealed class DogfoodBlock3Tests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public DogfoodBlock3Tests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-dogfood-block3");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 15000)]
    public async Task AdaptationEngine_DecomposeAndRecompileAshlarBrick_Succeeds()
    {
        var storePath = Path.Combine(_tempDir, "patterns.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(storePath)
            .BuildServiceProvider();

        var decomposer = services.GetRequiredService<IBrickDecomposer>();
        var recompiler = services.GetRequiredService<IBrickRecompiler>();
        var contextAssembler = services.GetRequiredService<IContextAssembler>();

        var brick = new ObservationContextBrick(contextAssembler);
        var manifest = await decomposer.DecomposeAsync(brick);

        Assert.NotNull(manifest);
        Assert.Equal("observation.context", manifest.Id);
        Assert.NotNull(manifest.ImplementationTypeName);
        Assert.Contains("ObservationContextBrick", manifest.ImplementationTypeName);

        var recompiled = await recompiler.RecompileAsync(manifest);
        Assert.NotNull(recompiled);
        Assert.Equal("observation.context", recompiled.Id);

        // Gate: Adaptation engine successfully processed (decompose + recompile) a Ashlar brick
    }
}
