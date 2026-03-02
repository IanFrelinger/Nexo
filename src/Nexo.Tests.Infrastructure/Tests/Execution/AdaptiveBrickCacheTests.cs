using FluentAssertions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Application.Networking.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for AdaptiveBrickCache: usage-aware brick caching (synaptic plasticity).
/// </summary>
public class AdaptiveBrickCacheTests
{
    private sealed class StubBrickRegistry : IBrickRegistry
    {
        private readonly Dictionary<string, Brick> _bricks = new(StringComparer.OrdinalIgnoreCase);

        public void Add(Brick b) => _bricks[b.Id] = b;

        public Brick? GetBrick(string id) => _bricks.TryGetValue(id, out var b) ? b : null;

        public IReadOnlyList<Brick> GetAllBricks() => _bricks.Values.ToList();
    }

    [Fact]
    public void GetBrick_DelegatesToInner_WhenNotCached()
    {
        var inner = new StubBrickRegistry();
        var brick = new TestBrick { Id = "b1", Name = "B1" };
        inner.Add(brick);

        var tracker = new BrickUsageTracker(Options.Create(new BrickUsageTrackerOptions()));
        var cache = new AdaptiveBrickCache(inner, tracker, Options.Create(new AdaptiveBrickCacheOptions()));

        var result = cache.GetBrick("b1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("b1");
        var stats = cache.GetCacheStats();
        stats.HitRate.Should().Be(0);
        stats.Entries.Should().Be(0);
    }

    [Fact]
    public void GetBrick_ReturnsNull_WhenInnerReturnsNull()
    {
        var inner = new StubBrickRegistry();
        var tracker = new BrickUsageTracker(Options.Create(new BrickUsageTrackerOptions()));
        var cache = new AdaptiveBrickCache(inner, tracker, Options.Create(new AdaptiveBrickCacheOptions()));

        var result = cache.GetBrick("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void GetCacheStats_ReturnsEntriesAndEvictionCount()
    {
        var inner = new StubBrickRegistry();
        inner.Add(new TestBrick { Id = "x", Name = "X" });

        var tracker = new BrickUsageTracker(Options.Create(new BrickUsageTrackerOptions()));
        var cache = new AdaptiveBrickCache(inner, tracker, Options.Create(new AdaptiveBrickCacheOptions()));

        cache.GetBrick("x");
        var stats = cache.GetCacheStats();

        stats.Entries.Should().Be(0);
        stats.EvictionCount.Should().Be(0);
    }

    private sealed class TestBrick : Brick
    {
        public override Task<BrickOutput> ExecuteAsync(BrickInput input, ImplementationType implementation, IExecutionContext context, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
