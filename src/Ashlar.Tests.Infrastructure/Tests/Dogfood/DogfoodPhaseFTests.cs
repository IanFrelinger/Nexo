using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.SelfContext.Models;
using Ashlar.Core.Application.SelfContext.Ports;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.SelfContext;
using Ashlar.Tests.Application.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Phase F: Continuous self-improvement loop. Validates changelog generation and test failure store.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
public sealed class DogfoodPhaseFTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public DogfoodPhaseFTests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-dogfood-phasef");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    [Fact(Timeout = 15000)]
    public async Task ChangelogGenerator_WithPromotedRecords_GeneratesMarkdown()
    {
        var storePath = Path.Combine(_tempDir, "ashlar-patterns.db");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(storePath)
            .AddChangelogGenerator()
            .BuildServiceProvider();

        var log = services.GetRequiredService<IAdaptationLog>();
        var generator = services.GetRequiredService<IChangelogGenerator>();

        await log.LogAsync(new AdaptationRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = "observation.context",
            FailureType = "EmptyCatch",
            FixApplied = AdaptationFixType.Source,
            FilePath = "/path/to/file.cs",
            RegressionPassed = true,
            Promoted = true,
            Message = "Fixed EmptyCatch in file.cs",
        });

        var changelog = await generator.GenerateAsync(
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Contains("# Changelog", changelog);
        Assert.Contains("observation.context", changelog);
        Assert.Contains("EmptyCatch", changelog);
    }

    [Fact(Timeout = 15000)]
    public async Task TestFailureStore_RecordAndQuery_ReturnsStoredFailures()
    {
        var dbPath = Path.Combine(_tempDir, "ashlar-test-failures.db");
        var store = new LiteDbTestFailureStore(dbPath);

        var record = new TestFailureRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            TestName = "DogfoodBlock1Tests.ObservationPipeline_WhenRunInAshlarRepo_StoresPatternsFromOwnFileEvents",
            FilePath = "src/Ashlar.Tests.Infrastructure/Tests/Dogfood/DogfoodBlock1Tests.cs",
            ErrorMessage = "Assert.True failed",
        };

        await store.RecordAsync(record);

        var since = DateTimeOffset.UtcNow.AddMinutes(-1);
        var results = await store.QueryAsync(since: since);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.TestName == record.TestName && r.Id == record.Id);
    }
}
