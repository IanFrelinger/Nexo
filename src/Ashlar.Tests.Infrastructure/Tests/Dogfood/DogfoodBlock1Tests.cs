using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Infrastructure.Observation;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 1 dogfood gate: verify observation pipeline watches a directory and stores patterns.
/// Uses a temp folder for watch path and verify file to avoid polluting the repo.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
public sealed class DogfoodBlock1Tests : TempDirTestBase
{
    public DogfoodBlock1Tests() : base("ashlar-dogfood-block1") { }

    [Fact(Timeout = 35000)]
    public async Task ObservationPipeline_WhenRunInAshlarRepo_StoresPatternsFromOwnFileEvents()
    {
        var storePath = Path.Combine(TempDir, "patterns.db");
        var verifyFile = Path.Combine(TempDir, "verify.tmp");

        var store = new LiteDbPatternStore(storePath);
        var patternDetector = new PatternDetector(
            TimeSpan.FromMinutes(5),
            3,
            store,
            null);

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        using var fileSource = new FileSystemEventSource(
            new[] { TempDir },
            TempDir,
            new[] { "*" },
            loggerFactory.CreateLogger<FileSystemEventSource>());
        fileSource.EnsureWatching();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var eventCount = 0;
        var processTask = Task.Run(async () =>
        {
            await foreach (var evt in fileSource.SubscribeAsync(cts.Token).WithCancellation(cts.Token))
            {
                eventCount++;
                await patternDetector.ProcessAsync(evt, cts.Token);
                if (eventCount >= 5)
                    break;
            }
        }, cts.Token);

        await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v1", cts.Token);
        await Task.Delay(500, cts.Token);
        await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v2", cts.Token);
        await Task.Delay(500, cts.Token);
        await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v3", cts.Token);
        await Task.Delay(500, cts.Token);
        await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v4", cts.Token);
        await Task.Delay(500, cts.Token);
        await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v5", cts.Token);

        await processTask;

        // Allow pattern detector to process events (FileSystemWatcher can batch/delay on macOS)
        await Task.Delay(1500, cts.Token);

        using var queryCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var patterns = await store.QueryAsync(new PatternStoreQueryParams { MaxCount = 10 }, queryCts.Token);

        var hasRepeatedEdits = patterns.Any(p => p.EventType == "repeated-edits");
        Assert.True(hasRepeatedEdits, $"Block 1 dogfood gate: Expected repeated-edits pattern from file events. Events: {eventCount}, Patterns: {patterns.Count}");
    }
}
