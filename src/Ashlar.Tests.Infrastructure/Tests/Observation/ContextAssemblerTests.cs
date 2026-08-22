using System.Collections.Concurrent;
using FluentAssertions;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Infrastructure.Observation;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Observation;

/// <summary>Tests for context assembler.</summary>
public class ContextAssemblerTests : IDisposable
{
    private readonly string _dbPath;

    public ContextAssemblerTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ashlar_context_test_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task AssembleContextAsync_EmptyStore_ReturnsEmptySummary()
    {
        var store = new LiteDbPatternStore(_dbPath);
        var assembler = new ContextAssembler(store);

        var context = await assembler.AssembleContextAsync(maxPatterns: 10);

        context.Patterns.Should().BeEmpty();
        context.Summary.Should().Contain("No observed patterns");
    }

    [Fact]
    public async Task AssembleContextAsync_WithPatterns_ReturnsSummary()
    {
        var store = new LiteDbPatternStore(_dbPath);
        await store.AddAsync(new ObservedPattern
        {
            PatternId = "p1",
            EventType = "repeated-edits",
            Frequency = 5,
            FirstSeen = DateTimeOffset.UtcNow.AddMinutes(-10),
            LastSeen = DateTimeOffset.UtcNow,
        });
        var assembler = new ContextAssembler(store);

        var context = await assembler.AssembleContextAsync(maxPatterns: 10);

        context.Patterns.Should().HaveCount(1);
        context.Summary.Should().Contain("1 pattern");
        context.Summary.Should().Contain("repeated-edits");
    }
}
