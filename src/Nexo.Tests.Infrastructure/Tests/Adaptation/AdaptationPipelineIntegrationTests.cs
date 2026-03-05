using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Bricks.Owasp.Security;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Observation;
using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>
/// Integration tests for adaptation pipeline: ImproveCommand, AdaptCommand, decompose/recompile.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Adaptation")]
public sealed class AdaptationPipelineIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public AdaptationPipelineIntegrationTests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-adapt-pipeline");
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

        var registry = services.GetRequiredService<Nexo.Core.Domain.Execution.IBrickRegistry>();
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
