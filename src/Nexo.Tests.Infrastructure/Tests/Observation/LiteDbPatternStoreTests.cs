using System.Text.Json;
using FluentAssertions;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Infrastructure.Observation;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Observation;

/// <summary>Tests for lite db pattern store.</summary>
public class LiteDbPatternStoreTests : TempDirTestBase
{
    private readonly string _dbPath;

    public LiteDbPatternStoreTests() : base("nexo-patterns-test")
    {
        _dbPath = Path.Combine(TempDir, "store.db");
    }

    [Fact]
    public void Ctor_WithPath_CreatesStore()
    {
        var store = new LiteDbPatternStore(_dbPath);
        store.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_NullOrEmpty_Throws()
    {
        var act = () => new LiteDbPatternStore("");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddAsync_AndQueryAsync_StoresAndRetrieves()
    {
        var store = new LiteDbPatternStore(_dbPath);
        var pattern = new ObservedPattern
        {
            PatternId = "p1",
            EventType = "repeated-edits",
            Frequency = 3,
            FirstSeen = DateTimeOffset.UtcNow.AddMinutes(-5),
            LastSeen = DateTimeOffset.UtcNow,
            ProjectPath = "/tmp/proj",
            RelatedEventIds = new[] { "e1", "e2", "e3" },
            Metadata = JsonSerializer.SerializeToElement(new { path = "src/foo.cs" }),
        };

        await store.AddAsync(pattern);

        var query = new PatternStoreQueryParams { MaxCount = 10 };
        var results = await store.QueryAsync(query);
        results.Should().HaveCount(1);
        results[0].PatternId.Should().Be("p1");
        results[0].EventType.Should().Be("repeated-edits");
        results[0].Frequency.Should().Be(3);
        results[0].ProjectPath.Should().Be("/tmp/proj");
    }

    [Fact]
    public async Task QueryAsync_WithEventTypeFilter_ReturnsMatchingOnly()
    {
        var store = new LiteDbPatternStore(_dbPath);
        await store.AddAsync(new ObservedPattern
        {
            PatternId = "p1",
            EventType = "repeated-edits",
            Frequency = 1,
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        });
        await store.AddAsync(new ObservedPattern
        {
            PatternId = "p2",
            EventType = "edit-then-build",
            Frequency = 1,
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        });

        var results = await store.QueryAsync(new PatternStoreQueryParams { EventType = "repeated-edits", MaxCount = 10 });
        results.Should().HaveCount(1);
        results[0].EventType.Should().Be("repeated-edits");
    }

    [Fact]
    public async Task QueryAsync_WithSince_ReturnsOnlyRecent()
    {
        var store = new LiteDbPatternStore(_dbPath);
        var old = DateTimeOffset.UtcNow.AddHours(-2);
        var recent = DateTimeOffset.UtcNow;
        await store.AddAsync(new ObservedPattern
        {
            PatternId = "old",
            EventType = "repeated-edits",
            Frequency = 1,
            FirstSeen = old,
            LastSeen = old,
        });
        await store.AddAsync(new ObservedPattern
        {
            PatternId = "new",
            EventType = "repeated-edits",
            Frequency = 1,
            FirstSeen = recent,
            LastSeen = recent,
        });

        var results = await store.QueryAsync(new PatternStoreQueryParams
        {
            Since = DateTimeOffset.UtcNow.AddHours(-1),
            MaxCount = 10,
        });
        results.Should().HaveCount(1);
        results[0].PatternId.Should().Be("new");
    }
}
