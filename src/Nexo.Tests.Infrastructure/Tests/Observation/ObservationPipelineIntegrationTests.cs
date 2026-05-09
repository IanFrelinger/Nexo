using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.Trust;
using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Observation;

/// <summary>
/// Integration tests for FileSystemEventSource → PatternDetector → LiteDbPatternStore pipeline.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class ObservationPipelineIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempDbPath;
    private readonly IDisposable _tempDirCleanup;

    public ObservationPipelineIntegrationTests()
    {
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-obs-pipeline");
        _tempDbPath = Path.Combine(_tempDir, "patterns.db");
    }

    public void Dispose()
    {
        _tempDirCleanup.Dispose();
    }

    /// <summary>Cross-process lock: Linux does not block on <see cref="FileShare.None"/>; other hosts get <see cref="IOException"/> and must poll.</summary>
    private static async Task<FileStream> AcquireCrossHostFileLockAsync(string path, TimeSpan maxWait, CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow + maxWait;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(
                    path,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.DeleteOnClose);
            }
            catch (IOException)
            {
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new TimeoutException($"Could not acquire exclusive lock for {path} within {maxWait}.");
    }

    [Fact(Timeout = TestTimeouts.FileSystemPipelineIntegration)]
    public async Task FullPipeline_FileSystemToPatternStore_StoresPatterns()
    {
        // Multi-target `dotnet test` runs net8 + net9 hosts in parallel; Linux inotify/FileSystemWatcher can drop events when both run this test at once.
        var gatePath = Path.Combine(Path.GetTempPath(), "nexo-observation-fs-pipeline.integration.lock");
        await using var crossHostGate = await AcquireCrossHostFileLockAsync(gatePath, TimeSpan.FromMinutes(3));
        var watchPath = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(watchPath);
        var testFile = Path.Combine(watchPath, "foo.cs");

        var store = new LiteDbPatternStore(_tempDbPath);
        var patternDetector = new PatternDetector(
            TimeSpan.FromMinutes(5),
            3,
            store,
            null);

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var fileSource = new FileSystemEventSource(
            new[] { watchPath },
            _tempDir,
            new[] { "*.cs" },
            loggerFactory.CreateLogger<FileSystemEventSource>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var eventCount = 0;
        var processTask = Task.Run(async () =>
        {
            await foreach (var evt in fileSource.SubscribeAsync(cts.Token).WithCancellation(cts.Token))
            {
                eventCount++;
                await patternDetector.ProcessAsync(evt, cts.Token);
                if (eventCount >= 12)
                    break;
            }
        }, cts.Token);

        await Task.Delay(500, CancellationToken.None);
        await File.WriteAllTextAsync(testFile, "// v1", cts.Token);
        await Task.Delay(500, cts.Token);
        await File.WriteAllTextAsync(testFile, "// v2", cts.Token);
        await Task.Delay(500, cts.Token);
        await File.WriteAllTextAsync(testFile, "// v3", cts.Token);

        await processTask;
        await Task.Delay(1000, CancellationToken.None);

        using var queryCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        IReadOnlyList<ObservedPattern> patterns = [];
        for (var attempt = 0; attempt < 30; attempt++)
        {
            patterns = await store.QueryAsync(new PatternStoreQueryParams { MaxCount = 10 }, queryCts.Token);
            if (patterns.Any(p => p.EventType == "repeated-edits"))
                break;
            await Task.Delay(200, queryCts.Token);
        }

        Assert.Contains(patterns, p => p.EventType == "repeated-edits");
        Assert.True(patterns.First(p => p.EventType == "repeated-edits").Frequency >= 3);
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task FullPipeline_WithObservationGate_FiltersEvents()
    {
        var watchPath = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(watchPath);
        var testFile = Path.Combine(watchPath, "bar.cs");

        var store = new LiteDbPatternStore(_tempDbPath);
        var patternDetector = new PatternDetector(
            TimeSpan.FromMinutes(5),
            3,
            store,
            null);

        var boundary = new AccessBoundary();
        boundary.SetCategoryAllowed("file-paths", false);
        var gate = new ObservationGate(boundary);

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var fileSource = new FileSystemEventSource(
            new[] { watchPath },
            _tempDir,
            new[] { "*.cs" },
            loggerFactory.CreateLogger<FileSystemEventSource>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var processTask = Task.Run(async () =>
        {
            await foreach (var evt in fileSource.SubscribeAsync(cts.Token).WithCancellation(cts.Token))
            {
                if (gate.ShouldObserve(evt.Category, evt.SourceId, evt.ProjectPath))
                    await patternDetector.ProcessAsync(evt, cts.Token);
            }
        }, cts.Token);

        await File.WriteAllTextAsync(testFile, "// v1", cts.Token);
        await Task.Delay(200, cts.Token);
        await File.WriteAllTextAsync(testFile, "// v2", cts.Token);
        await Task.Delay(200, cts.Token);
        await File.WriteAllTextAsync(testFile, "// v3", cts.Token);

        await processTask;

        using var queryCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var patterns = await store.QueryAsync(new PatternStoreQueryParams { MaxCount = 10 }, queryCts.Token);
        Assert.Empty(patterns);
    }
}
