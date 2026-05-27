using FluentAssertions;
using Microsoft.Extensions.Options;
using Nexo.BrickContracts;
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

    [Fact]
    public void GetBrick_empty_id_returns_null()
    {
        var inner = new StubBrickRegistry();
        var tracker = new BrickUsageTracker(Options.Create(new BrickUsageTrackerOptions()));
        var cache = new AdaptiveBrickCache(inner, tracker, Options.Create(new AdaptiveBrickCacheOptions()));

        cache.GetBrick("").Should().BeNull();
        cache.GetBrick("   ").Should().BeNull();
    }

    [Fact]
    public void GetBrick_caches_remote_bricks_and_reports_hits()
    {
        var inner = new StubBrickRegistry();
        inner.Add(CreateRemoteBrick("remote-1"));

        var tracker = new BrickUsageTracker(Options.Create(new BrickUsageTrackerOptions()));
        var cache = new AdaptiveBrickCache(inner, tracker, Options.Create(new AdaptiveBrickCacheOptions
        {
            ColdBrickTtlSeconds = 60,
            HotBrickTtlSeconds = 120,
        }));

        cache.GetBrick("remote-1").Should().NotBeNull();
        cache.GetBrick("remote-1").Should().NotBeNull();

        var stats = cache.GetCacheStats();
        stats.Entries.Should().Be(1);
        stats.HitRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBrick_evicts_expired_remote_entries()
    {
        var inner = new StubBrickRegistry();
        inner.Add(CreateRemoteBrick("remote-expire"));

        var tracker = new BrickUsageTracker(Options.Create(new BrickUsageTrackerOptions()));
        var cache = new AdaptiveBrickCache(inner, tracker, Options.Create(new AdaptiveBrickCacheOptions
        {
            ColdBrickTtlSeconds = 0,
            HotBrickTtlSeconds = 0,
        }));

        cache.GetBrick("remote-expire");
        await Task.Delay(15);
        cache.GetBrick("remote-expire");

        cache.GetCacheStats().EvictionCount.Should().BeGreaterThan(0);
    }

    private static RemoteBrick CreateRemoteBrick(string id)
    {
        var entry = new Nexo.BrickContracts.BrickCatalogEntryDto
        {
            Id = id,
            Name = id,
            Category = "Control",
            Description = "remote test brick",
            HostBaseUrl = "http://127.0.0.1:9",
        };
        return new RemoteBrick(entry, new HttpClient(), "http://127.0.0.1:9");
    }

    private sealed class TestBrick : Brick
    {
        public override Task<BrickOutput> ExecuteAsync(BrickInput input, ImplementationType implementation, IExecutionContext context, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
