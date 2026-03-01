using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Infrastructure.Observation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Observation;

public sealed class CompositeEventSourceTests
{
    [Fact]
    public async Task SubscribeAsync_WithEmptySources_YieldsNothing()
    {
        var composite = new CompositeEventSource(Array.Empty<IObservableEventSource>());
        var collected = new List<NormalizedEvent>();

        await foreach (var evt in composite.SubscribeAsync(CancellationToken.None).WithCancellation(CancellationToken.None))
        {
            collected.Add(evt);
        }

        collected.Should().BeEmpty();
    }

    [Fact]
    public async Task SubscribeAsync_WithMockSource_YieldsMergedEvents()
    {
        var evt1 = new NormalizedEvent
        {
            EventId = "e1",
            Timestamp = DateTimeOffset.UtcNow,
            SourceId = "mock",
            Category = "test",
            Payload = JsonSerializer.SerializeToElement(new { id = 1 }),
        };
        var evt2 = new NormalizedEvent
        {
            EventId = "e2",
            Timestamp = DateTimeOffset.UtcNow,
            SourceId = "mock",
            Category = "test",
            Payload = JsonSerializer.SerializeToElement(new { id = 2 }),
        };

        var mockSource = new MockEventSource("mock", [evt1, evt2]);
        var composite = new CompositeEventSource([mockSource]);
        var collected = new List<NormalizedEvent>();

        await foreach (var evt in composite.SubscribeAsync(CancellationToken.None).WithCancellation(CancellationToken.None))
        {
            collected.Add(evt);
            if (collected.Count >= 2) break;
        }

        collected.Should().HaveCount(2);
        collected[0].EventId.Should().Be("e1");
        collected[1].EventId.Should().Be("e2");
    }

    private sealed class MockEventSource : IObservableEventSource
    {
        private readonly NormalizedEvent[] _events;

        public MockEventSource(string sourceId, NormalizedEvent[] events)
        {
            SourceId = sourceId;
            _events = events;
        }

        public string SourceId { get; }

        public async IAsyncEnumerable<NormalizedEvent> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var evt in _events)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return evt;
            }
        }
    }
}
