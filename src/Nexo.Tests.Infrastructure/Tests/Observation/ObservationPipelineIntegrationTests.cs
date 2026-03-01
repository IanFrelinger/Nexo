using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.Trust;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Observation;

/// <summary>
/// Integration tests for FileSystemEventSource → PatternDetector → LiteDbPatternStore pipeline.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
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

    [Fact]
    public async Task FullPipeline_FileSystemToPatternStore_StoresPatterns()
    {
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

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var eventCount = 0;
        var processTask = Task.Run(async () =>
        {
            await foreach (var evt in fileSource.SubscribeAsync(cts.Token).WithCancellation(cts.Token))
            {
                eventCount++;
                await patternDetector.ProcessAsync(evt, cts.Token);
                if (eventCount >= 3)
                    break;
            }
        }, cts.Token);

        await File.WriteAllTextAsync(testFile, "// v1", cts.Token);
        await Task.Delay(200, cts.Token);
        await File.WriteAllTextAsync(testFile, "// v2", cts.Token);
        await Task.Delay(200, cts.Token);
        await File.WriteAllTextAsync(testFile, "// v3", cts.Token);

        await processTask;

        var patterns = await store.QueryAsync(new PatternStoreQueryParams { MaxCount = 10 }, cts.Token);
        Assert.Contains(patterns, p => p.EventType == "repeated-edits");
        Assert.True(patterns.First(p => p.EventType == "repeated-edits").Frequency >= 3);
    }

    [Fact]
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
