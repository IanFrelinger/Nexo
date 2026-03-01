using FluentAssertions;
using Nexo.Infrastructure.Observation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Observation;

public class FileSystemEventSourceTests : IDisposable
{
    private readonly string _tempDir;

    public FileSystemEventSourceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"nexo_observe_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

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
        var events = new List<Nexo.Core.Application.Observation.Models.NormalizedEvent>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

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
