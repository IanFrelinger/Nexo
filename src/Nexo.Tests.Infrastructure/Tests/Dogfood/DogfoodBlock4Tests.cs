using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Infrastructure;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 4 dogfood gate: promote one Nexo fix via inheritance.
/// Validates IAdaptationPromoter records a promoted fix for a Nexo brick.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
[Trait("Category", "Adaptation")]
public sealed class DogfoodBlock4Tests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public DogfoodBlock4Tests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-dogfood-block4");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 15000)]
    public async Task PromoteNexoFix_ViaInheritance_RecordStoredWithPromotedTrue()
    {
        var storePath = Path.Combine(_tempDir, "patterns.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(storePath)
            .BuildServiceProvider();

        var promoter = services.GetRequiredService<IAdaptationPromoter>();
        var log = services.GetRequiredService<IAdaptationLog>();

        var record = new AdaptationRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = "observation.context",
            FailureType = "EmptyCatch",
            FixApplied = AdaptationFixType.Source,
            FilePath = Path.Combine(_tempDir, "ObservationContextBrick.cs"),
            RegressionPassed = true,
            Promoted = false,
            Message = "Dogfood: promoted Nexo fix via inheritance",
        };

        await promoter.PromoteAsync(record);

        var records = await log.QueryAsync(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1), "observation.context");
        Assert.NotEmpty(records);
        var promotedRecord = records.FirstOrDefault(r => r.Id == record.Id);
        Assert.NotNull(promotedRecord);
        Assert.True(promotedRecord.Promoted);
        Assert.Equal("observation.context", promotedRecord.BrickId);
    }
}
