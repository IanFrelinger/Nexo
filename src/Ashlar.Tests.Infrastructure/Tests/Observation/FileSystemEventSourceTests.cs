using FluentAssertions;
using Ashlar.Infrastructure.Observation;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Observation;

/// <summary>Tests for file system event source.</summary>
public class FileSystemEventSourceTests : IDisposable
{
    private readonly string _tempDir;

    public FileSystemEventSourceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ashlar_observe_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>Dispose.</summary>
    public void Dispose() => Dispose(true);
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public async Task SubscribeAsync_FileCreated_EmitsEvent()
    {
        var source = new FileSystemEventSource(new[] { _tempDir }, _tempDir, new[] { "*" }, null);
        var events = new List<Ashlar.Core.Application.Observation.Models.NormalizedEvent>();
        // Hang net, not a performance budget: healthy runs finish in under a second, but
        // under kernel-coverage-gate instrumentation this test's file write once executed
        // 6m17s late and a 20s token turned that stall into a red build (see
        // docs/production-readiness/KernelCoverageGate-Findings.md).
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestTimeouts.HostTouching));

        var consumeTask = Task.Run(async () =>
        {
            await foreach (var evt in source.SubscribeAsync(cts.Token))
            {
                events.Add(evt);
                if (events.Count >= 1) break;
            }
        }, cts.Token);

        await Task.Delay(200, cts.Token);
        var filePath = Path.Combine(_tempDir, "test.txt");
        await File.WriteAllTextAsync(filePath, "hello", cts.Token);

        await consumeTask;

        events.Should().ContainSingle();
        events[0].SourceId.Should().Be("file-system");
        events[0].Category.Should().Be("file-paths");
    }

    [Fact]
    public void Ctor_EmptyWatchPaths_DoesNotThrow()
    {
        var source = new FileSystemEventSource(Array.Empty<string>(), null);
        source.SourceId.Should().Be("file-system");
    }
}
