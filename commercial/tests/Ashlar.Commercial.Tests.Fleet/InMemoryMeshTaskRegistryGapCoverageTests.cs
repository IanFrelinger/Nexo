using FluentAssertions;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for in memory mesh task registry gap coverage.</summary>
public sealed class InMemoryMeshTaskRegistryGapCoverageTests
{
    [Fact]
    public async Task GetAsync_returns_null_for_missing_task()
    {
        var registry = new InMemoryMeshTaskRegistry();

        (await registry.GetAsync("missing")).Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_returns_false_for_unknown_task()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var orphan = await registry.CreateAsync(new MeshTaskCreateSpec("orphan", 1, [], null, 0, null));

        var updated = await registry.UpdateAsync(orphan with { TaskId = "unknown-id", Name = "changed" });

        updated.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_clamps_steps_to_at_least_one()
    {
        var registry = new InMemoryMeshTaskRegistry();

        var task = await registry.CreateAsync(new MeshTaskCreateSpec("zero-steps", 0, [], null, 0, null));

        task.Steps.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_trims_correlation_and_idempotency_keys()
    {
        var registry = new InMemoryMeshTaskRegistry();

        var task = await registry.CreateAsync(new MeshTaskCreateSpec(
            "trim",
            1,
            [],
            null,
            0,
            null,
            CorrelationId: "  corr-1  ",
            IdempotencyKey: "  idem-1  "));

        task.CorrelationId.Should().Be("corr-1");
        task.IdempotencyKey.Should().Be("idem-1");
    }

    [Fact]
    public async Task TryGetByIdempotencyKeyAsync_returns_null_for_blank_key()
    {
        var registry = new InMemoryMeshTaskRegistry();

        (await registry.TryGetByIdempotencyKeyAsync("")).Should().BeNull();
        (await registry.TryGetByIdempotencyKeyAsync("   ")).Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_orders_by_priority_then_created_time()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var low = await registry.CreateAsync(new MeshTaskCreateSpec("low", 1, [], null, Priority: 1, DeadlineUtc: null));
        await Task.Delay(5);
        var high = await registry.CreateAsync(new MeshTaskCreateSpec("high", 1, [], null, Priority: 10, DeadlineUtc: null));

        var listed = await registry.ListAsync();

        listed.Select(t => t.TaskId).Should().ContainInOrder(high.TaskId, low.TaskId);
    }

    [Fact]
    public async Task UpdateAsync_persists_mutations_for_existing_task()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("mutate", 1, [], null, 0, null));

        var updated = created with { Status = MeshTaskStatus.Running, ResultSummary = "half-done" };
        (await registry.UpdateAsync(updated)).Should().BeTrue();

        (await registry.GetAsync(created.TaskId))!.ResultSummary.Should().Be("half-done");
    }
}
