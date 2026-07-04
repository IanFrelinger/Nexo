using FluentAssertions;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Tests for lite db mesh task registry gap coverage.</summary>
[Collection(nameof(LiteDbFleetCollection))]
public sealed class LiteDbMeshTaskRegistryGapCoverageTests
{
    [Fact]
    public void Constructor_rejects_blank_database_path()
    {
        var act = () => new LiteDbMeshTaskRegistry("   ");

        act.Should().Throw<ArgumentNullException>().WithParameterName("pathOrConnectionString");
    }

    [Fact]
    public async Task CreateAsync_deduplicates_by_idempotency_key()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbMeshTaskRegistry(path);
            var spec = new MeshTaskCreateSpec("idem", 1, [], null, 0, null, IdempotencyKey: "lite-idem");

            var first = await registry.CreateAsync(spec);
            var second = await registry.CreateAsync(spec);

            first.TaskId.Should().Be(second.TaskId);
            (await registry.TryGetByIdempotencyKeyAsync("lite-idem"))!.TaskId.Should().Be(first.TaskId);
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public async Task GetAsync_returns_null_for_missing_task()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbMeshTaskRegistry(path);

            (await registry.GetAsync("missing")).Should().BeNull();
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public async Task UpdateAsync_returns_false_for_unknown_task()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbMeshTaskRegistry(path);
            var created = await registry.CreateAsync(new MeshTaskCreateSpec("orphan", 1, [], null, 0, null));

            var updated = await registry.UpdateAsync(created with { TaskId = "unknown", Name = "changed" });

            updated.Should().BeFalse();
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public async Task CreateAsync_clamps_steps_and_trims_keys()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbMeshTaskRegistry(path);

            var task = await registry.CreateAsync(new MeshTaskCreateSpec(
                "trim",
                0,
                [],
                null,
                0,
                null,
                CorrelationId: "  corr-lite  ",
                IdempotencyKey: "  idem-lite  "));

            task.Steps.Should().Be(1);
            task.CorrelationId.Should().Be("corr-lite");
            task.IdempotencyKey.Should().Be("idem-lite");
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public async Task TryGetByIdempotencyKeyAsync_returns_null_for_blank_key()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbMeshTaskRegistry(path);

            (await registry.TryGetByIdempotencyKeyAsync("")).Should().BeNull();
            (await registry.TryGetByIdempotencyKeyAsync("   ")).Should().BeNull();
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public async Task ListAsync_orders_by_priority_then_created_time()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbMeshTaskRegistry(path);
            var low = await registry.CreateAsync(new MeshTaskCreateSpec("low", 1, [], null, Priority: 1, DeadlineUtc: null));
            await Task.Delay(5);
            var high = await registry.CreateAsync(new MeshTaskCreateSpec("high", 1, [], null, Priority: 10, DeadlineUtc: null));

            var listed = await registry.ListAsync();

            listed.Select(t => t.TaskId).Should().ContainInOrder(high.TaskId, low.TaskId);
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public async Task UpdateAsync_persists_mutations_for_existing_task()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbMeshTaskRegistry(path);
            var created = await registry.CreateAsync(new MeshTaskCreateSpec("mutate", 1, [], null, 0, null));

            var updated = created with { Status = MeshTaskStatus.Running, ResultSummary = "half-done" };
            (await registry.UpdateAsync(updated)).Should().BeTrue();

            (await registry.GetAsync(created.TaskId))!.ResultSummary.Should().Be("half-done");
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public void Accepts_filename_connection_string()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbMeshTaskRegistry($"Filename={path}");

            registry.Should().NotBeNull();
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    private static string CreateTempDbPath()
        => Path.Combine(Path.GetTempPath(), "nexo-lite-task-" + Guid.NewGuid().ToString("N") + ".db");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}
