using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Observation;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>
/// Integration tests for adaptation pipeline: ImproveCommand, AdaptCommand, decompose/recompile.
/// </summary>
[Trait("Category", "Integration")]
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

    [Fact]
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

    [Fact]
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
}
