using FluentAssertions;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Xunit;

namespace Nexo.Tests.Contracts;

/// <summary>
/// Contract tests for IPatternStore implementations.
/// Every implementation must pass these tests to ensure consistent behavior.
/// </summary>
public abstract class PatternStoreContractTests
{
    protected abstract IPatternStore CreateInstance();

    [Fact]
    public async Task AddAsync_QueryAsync_ReturnsAddedPattern()
    {
        var store = CreateInstance();
        var pattern = new ObservedPattern
        {
            PatternId = "p1",
            EventType = "repeated-edits",
            Frequency = 3,
            FirstSeen = DateTimeOffset.UtcNow.AddHours(-1),
            LastSeen = DateTimeOffset.UtcNow,
            ProjectPath = "src",
            Metadata = System.Text.Json.JsonSerializer.SerializeToElement(new { path = "src/foo.cs" }),
        };

        await store.AddAsync(pattern);
        var results = await store.QueryAsync(new PatternStoreQueryParams
        {
            Since = DateTimeOffset.UtcNow.AddHours(-2),
            EventType = "repeated-edits",
            MaxCount = 10,
        });

        results.Should().ContainSingle();
        results[0].PatternId.Should().Be("p1");
    }

    [Fact]
    public async Task QueryAsync_WithEventTypeFilter_ReturnsOnlyMatching()
    {
        var store = CreateInstance();
        await store.AddAsync(new ObservedPattern
        {
            PatternId = "p-a",
            EventType = "repeated-edits",
            Frequency = 2,
            FirstSeen = DateTimeOffset.UtcNow.AddHours(-1),
            LastSeen = DateTimeOffset.UtcNow,
            ProjectPath = "src",
        });
        await store.AddAsync(new ObservedPattern
        {
            PatternId = "p-b",
            EventType = "edit-then-build",
            Frequency = 1,
            FirstSeen = DateTimeOffset.UtcNow.AddHours(-1),
            LastSeen = DateTimeOffset.UtcNow,
            ProjectPath = "src",
        });

        var results = await store.QueryAsync(new PatternStoreQueryParams
        {
            Since = DateTimeOffset.UtcNow.AddHours(-2),
            EventType = "repeated-edits",
            MaxCount = 10,
        });

        results.Should().OnlyContain(p => p.EventType == "repeated-edits");
    }
}
