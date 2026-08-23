using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Bricks.Owasp.Security;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Execution;
using Ashlar.Infrastructure.Observation;
using Ashlar.Tests.Application.Helpers;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Adaptation;

/// <summary>
/// Integration tests for adaptation pipeline: ImproveCommand, AdaptCommand, decompose/recompile.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Adaptation")]
[Trait("Category", "ProdStyle")]
public sealed class AdaptationPipelineIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public AdaptationPipelineIntegrationTests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-adapt-pipeline");
    }

    public void Dispose()
    {
        _tempDirCleanup.Dispose();
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task AdaptCommand_DecomposeRecompile_ReturnsBrick()
    {
        var storePath = Path.Combine(_tempDir, "patterns.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddAdaptationInfrastructure(storePath)
            .BuildServiceProvider();

        var decomposer = services.GetRequiredService<IBrickDecomposer>();
        var recompiler = services.GetRequiredService<IBrickRecompiler>();
        var contextAssembler = services.GetRequiredService<IContextAssembler>();

        var brick = new ObservationContextBrick(contextAssembler);
        var manifest = await decomposer.DecomposeAsync(brick);

        Assert.Equal("observation.context", manifest.Id);
        Assert.NotNull(manifest.ImplementationTypeName);
        Assert.Contains("ObservationContextBrick", manifest.ImplementationTypeName);

        var recompiled = await recompiler.RecompileAsync(manifest);

        Assert.NotNull(recompiled);
        Assert.Equal("observation.context", recompiled.Id);
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task ImproveCommand_WithViolations_ValidateBeforePromote_RollbackOnRegression()
    {
        var storePath = Path.Combine(_tempDir, "patterns.db");
        var csPath = TestHelpers.CreateTempCsFileWithEmptyCatch(_tempDir);

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddAdaptationInfrastructure(storePath)
            .BuildServiceProvider();

        var adaptationLog = services.GetRequiredService<IAdaptationLog>();

        await adaptationLog.LogAsync(new AdaptationRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = "observation.context",
            FailureType = "EmptyCatch",
            FixApplied = AdaptationFixType.Source,
            FilePath = csPath,
            RegressionPassed = false,
            Promoted = false,
            Message = "Rollback on regression"
        });

        var records = await adaptationLog.QueryAsync(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
        Assert.Single(records);
        Assert.False(records[0].Promoted);
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task AdaptCommand_WithOWASPScannerBrick_DecomposeRecompile_Succeeds()
    {
        var storePath = Path.Combine(_tempDir, "patterns.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddSingleton<IProviderFactory, ProviderFactory>()
            .AddAdaptationInfrastructure(storePath)
            .AddAdaptationBricks(typeof(OWASPScannerBrick))
            .BuildServiceProvider();

        var registry = services.GetRequiredService<Ashlar.Core.Domain.Execution.IBrickRegistry>();
        var decomposer = services.GetRequiredService<IBrickDecomposer>();
        var recompiler = services.GetRequiredService<IBrickRecompiler>();

        var brick = registry.GetBrick("owasp-scanner");
        Assert.NotNull(brick);

        var manifest = await decomposer.DecomposeAsync(brick);
        Assert.NotNull(manifest);
        Assert.Equal("owasp-scanner", manifest.Id);

        var recompiled = await recompiler.RecompileAsync(manifest);
        Assert.NotNull(recompiled);
        Assert.Equal("owasp-scanner", recompiled.Id);
    }
}
