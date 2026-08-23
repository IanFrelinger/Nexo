using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Paths;
using Ashlar.Infrastructure;
using Ashlar.Tests.Application.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 5 dogfood gate: use autonomy controls on Ashlar dev workflow.
/// Validates IAdaptationAuditLog records adaptation attempts with autonomy level for Ashlar files.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
[Trait("Category", "Adaptation")]
public sealed class DogfoodBlock5Tests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public DogfoodBlock5Tests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-dogfood-block5");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 15000)]
    public async Task AutonomyControls_LogAshlarDevWorkflowAdaptation_EntryStoredAndQueryable()
    {
        var storePath = Path.Combine(_tempDir, "patterns.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(storePath)
            .BuildServiceProvider();

        var auditLog = services.GetRequiredService<IAdaptationAuditLog>();
        var ashlarSrcPath = RepoPathResolver.FindBlock1ObservationPath();

        var entry = new AdaptationAuditEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            AutonomyLevel = "supervised",
            Outcome = "Promoted",
            BrickId = "observation.context",
            FailureType = "EmptyCatch",
            FilePath = Path.Combine(ashlarSrcPath, "ContextAssembler.cs"),
            RegressionPassed = true,
            Promoted = true,
            Message = "Dogfood: autonomy controls used on Ashlar dev workflow",
        };

        await auditLog.LogAsync(entry);

        var entries = await auditLog.QueryAsync(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.NotEmpty(entries);
        var found = entries.FirstOrDefault(e => e.Id == entry.Id);
        Assert.NotNull(found);
        Assert.Equal("supervised", found.AutonomyLevel);
        Assert.Equal("observation.context", found.BrickId);
        Assert.True(found.Promoted);
    }
}
