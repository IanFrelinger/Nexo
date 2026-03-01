using System.Collections.Concurrent;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Infrastructure.Observation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Observation;

public class PatternDetectorTests
{
    private sealed class InMemoryPatternStore : IPatternStore
    {
        private readonly ConcurrentBag<ObservedPattern> _patterns = new();

        public Task AddAsync(ObservedPattern pattern, CancellationToken cancellationToken = default)
        {
            _patterns.Add(pattern);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ObservedPattern>> QueryAsync(PatternStoreQueryParams query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ObservedPattern>>(_patterns.ToList());
        }

        public Task PersistAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public IReadOnlyList<ObservedPattern> GetAll() => _patterns.ToList();
    }

    [Fact]
    public async Task ProcessAsync_RepeatedEditsToSamePath_EmitsRepeatedEditsPattern()
    {
        var store = new InMemoryPatternStore();
        var detector = new PatternDetector(TimeSpan.FromMinutes(5), 3, store, null);

        var path = "/tmp/proj/src/foo.cs";
        var payload = JsonSerializer.SerializeToElement(new { fullPath = path, changeType = "changed" });

        for (var i = 0; i < 3; i++)
        {
            await detector.ProcessAsync(new NormalizedEvent
            {
                EventId = $"e{i}",
                Timestamp = DateTimeOffset.UtcNow,
                SourceId = "file-system",
                Category = "file-paths",
                Payload = payload,
            });
        }

        var patterns = store.GetAll();
        patterns.Should().ContainSingle(p => p.EventType == "repeated-edits");
        patterns.First(p => p.EventType == "repeated-edits").Frequency.Should().Be(3);
    }

    [Fact]
    public async Task ProcessAsync_BelowThreshold_DoesNotEmit()
    {
        var store = new InMemoryPatternStore();
        var detector = new PatternDetector(TimeSpan.FromMinutes(5), 3, store, null);

        var path = "/tmp/proj/src/foo.cs";
        var payload = JsonSerializer.SerializeToElement(new { fullPath = path, changeType = "changed" });

        for (var i = 0; i < 2; i++)
        {
            await detector.ProcessAsync(new NormalizedEvent
            {
                EventId = $"e{i}",
                Timestamp = DateTimeOffset.UtcNow,
                SourceId = "file-system",
                Category = "file-paths",
                Payload = payload,
            });
        }

        store.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_ProcessEvents_DoesNotCrash()
    {
        var store = new InMemoryPatternStore();
        var detector = new PatternDetector(TimeSpan.FromMinutes(5), 3, store, null);

        var payload = JsonSerializer.SerializeToElement(new { processName = "dotnet", changeType = "started" });
        await detector.ProcessAsync(new NormalizedEvent
        {
            EventId = "pe1",
            Timestamp = DateTimeOffset.UtcNow,
            SourceId = "process-monitor",
            Category = "process-names",
            Payload = payload,
        });
    }
}
