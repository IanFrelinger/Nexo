using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Runtime;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Runtime;

public sealed class InMemoryAgentMemoryTests
{
    [Fact(Timeout = 15000)]
    public void Write_ThenQuery_ReturnsMatchingEvents()
    {
        var memory = new InMemoryAgentMemory();

        memory.Write(new EventRecord(DateTimeOffset.UtcNow, "agent-1", "info", "Hello world"));
        memory.Write(new EventRecord(DateTimeOffset.UtcNow, "agent-1", "error", "Something failed"));

        var results = memory.Query("Hello", k: 10);

        results.Should().HaveCount(1);
        results[0].Message.Should().Be("Hello world");
    }

    [Fact(Timeout = 15000)]
    public void Query_IsCaseInsensitive()
    {
        var memory = new InMemoryAgentMemory();
        memory.Write(new EventRecord(DateTimeOffset.UtcNow, "a", "info", "Build Succeeded"));

        var results = memory.Query("build succeeded", k: 10);

        results.Should().HaveCount(1);
    }

    [Fact(Timeout = 15000)]
    public void Query_RespectsKLimit()
    {
        var memory = new InMemoryAgentMemory();

        for (var i = 0; i < 10; i++)
        {
            memory.Write(new EventRecord(
                DateTimeOffset.UtcNow.AddSeconds(i), "a", "info", $"Event match {i}"));
        }

        var results = memory.Query("match", k: 3);

        results.Should().HaveCount(3);
    }

    [Fact(Timeout = 15000)]
    public void Query_OrdersByTimestampDescending()
    {
        var memory = new InMemoryAgentMemory();
        var t1 = DateTimeOffset.UtcNow.AddMinutes(-5);
        var t2 = DateTimeOffset.UtcNow.AddMinutes(-1);
        var t3 = DateTimeOffset.UtcNow;

        memory.Write(new EventRecord(t1, "a", "info", "Event old"));
        memory.Write(new EventRecord(t3, "a", "info", "Event newest"));
        memory.Write(new EventRecord(t2, "a", "info", "Event middle"));

        var results = memory.Query("Event", k: 10);

        results.Should().HaveCount(3);
        results[0].At.Should().Be(t3);
        results[1].At.Should().Be(t2);
        results[2].At.Should().Be(t1);
    }

    [Fact(Timeout = 15000)]
    public void Query_NoMatches_ReturnsEmpty()
    {
        var memory = new InMemoryAgentMemory();
        memory.Write(new EventRecord(DateTimeOffset.UtcNow, "a", "info", "Alpha"));

        var results = memory.Query("Zeta", k: 10);

        results.Should().BeEmpty();
    }
}
