using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Infrastructure.Observation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Dogfood;

/// <summary>
/// Block 1 dogfood gate: verify observation pipeline watches Nexo's own dev workflow and stores patterns.
/// When run from the Nexo repo, the pipeline must observe file events under src/ and persist patterns.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "Dogfood")]
public sealed class DogfoodBlock1Tests
{
    [Fact(Timeout = 25000)]
    public async Task ObservationPipeline_WhenRunInNexoRepo_StoresPatternsFromOwnFileEvents()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Nexo.sln");
        if (!File.Exists(slnPath))
        {
            // Not in Nexo repo — skip (e.g. in CI or when repo structure differs)
            return;
        }

        var srcDir = Path.Combine(repoRoot, "src");
        if (!Directory.Exists(srcDir))
        {
            return;
        }

        var storePath = Path.Combine(Path.GetTempPath(), $"nexo-dogfood-block1-{Guid.NewGuid():N}.db");
        var verifyFile = Path.Combine(srcDir, ".dogfood-block1-verify.tmp");

        try
        {
            var store = new LiteDbPatternStore(storePath);
            var patternDetector = new PatternDetector(
                TimeSpan.FromMinutes(5),
                3,
                store,
                null);

            var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
            var fileSource = new FileSystemEventSource(
                new[] { srcDir },
                repoRoot,
                new[] { "*" },
                loggerFactory.CreateLogger<FileSystemEventSource>());

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
            await Task.Delay(300, cts.Token);
            await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v2", cts.Token);
            await Task.Delay(300, cts.Token);
            await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v3", cts.Token);

            await processTask;

            using var queryCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var patterns = await store.QueryAsync(new PatternStoreQueryParams { MaxCount = 10 }, queryCts.Token);

            var hasRepeatedEdits = patterns.Any(p => p.EventType == "repeated-edits");
            Assert.True(hasRepeatedEdits, $"Block 1 dogfood gate: Expected repeated-edits pattern from Nexo's own file events. Events: {eventCount}, Patterns: {patterns.Count}");
        }
        finally
        {
            try
            {
                if (File.Exists(verifyFile))
                    File.Delete(verifyFile);
                if (File.Exists(storePath))
                    File.Delete(storePath);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}
