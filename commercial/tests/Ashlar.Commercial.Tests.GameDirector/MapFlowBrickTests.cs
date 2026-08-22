using FluentAssertions;
using GameDirector.Bricks;
using GameDirector.Bricks.Schemas;
using GameDirector.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Xunit;

namespace Ashlar.Tests.GameDirector;

/// <summary>Tests for map flow brick.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class MapFlowBrickTests
{
    [Fact]
    public void Brick_contract_declares_map_flow_outputs()
    {
        var brick = new MapFlowBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<MapFlowBrick>.Instance);
        brick.Id.Should().Be("map.flow");
        brick.Category.Should().Be(BrickCategory.Validation);
        brick.Interface.Outputs.Select(o => o.Name).Should().BeEquivalentTo(
            ["audit_id", "choke_density", "spawn_equity", "sightline_report", "recommendations"]);
    }

    [Fact]
    public async Task High_choke_density_produces_recommendation()
    {
        var brick = new MapFlowBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<MapFlowBrick>.Instance);
        var input = GameDirectorTestHost.MapInput("choke_arena", chokeTiles: 4, openTiles: 1, spawnCount: 4);

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<double>("choke_density").Should().BeGreaterThan(0.25);
        output.Get<List<string>>("recommendations").Should().Contain(r => r.Contains("choke", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Balanced_map_has_acceptable_metrics_message()
    {
        var brick = new MapFlowBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<MapFlowBrick>.Instance);
        var tiles = new[]
        {
            new[] { new { x = 0, y = 0, traversalOptions = 4 }, new { x = 1, y = 0, traversalOptions = 4 } },
            new[] { new { x = 0, y = 1, traversalOptions = 4 }, new { x = 1, y = 1, traversalOptions = 4 } }
        };
        var spawns = new[]
        {
            new { id = "a", x = 0, y = 0 },
            new { id = "b", x = 10, y = 10 },
            new { id = "c", x = 20, y = 5 },
            new { id = "d", x = 5, y = 20 }
        };
        var input = new BrickInput();
        input.Set("map_id", "balanced");
        input.Set("config", System.Text.Json.JsonSerializer.SerializeToElement(new { tiles, spawns }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<double>("choke_density").Should().BeLessThan(0.25);
        output.Get<List<string>>("recommendations").Should().Contain(r => r.Contains("acceptable", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Few_spawns_recommends_adding_spawn_points()
    {
        var brick = new MapFlowBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<MapFlowBrick>.Instance);
        var input = GameDirectorTestHost.MapInput("duel", chokeTiles: 1, openTiles: 2, spawnCount: 2);

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<List<string>>("recommendations").Should().Contain(r => r.Contains("spawn", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Model_overrides_sightline_report_when_provided()
    {
        var model = new GameDirectorTestHost.CapturingModel();
        var brick = new MapFlowBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<MapFlowBrick>.Instance, model);
        var input = GameDirectorTestHost.MapInput("sight_test");

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<string>("sightline_report").Should().Be("captured-model-output");
    }

    [Fact]
    public async Task Audit_log_records_validate_map_tool()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var brick = new MapFlowBrick(audit, NullLogger<MapFlowBrick>.Instance);
        var input = GameDirectorTestHost.MapInput("audit_map");

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        var records = AuditRecordParser.Query(
            audit,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(1),
            tool: "validate_map");
        records.Should().Contain(r => r.AuditId == output.Get<string>("audit_id"));
    }

    [Fact]
    public async Task Loads_sample_mapconfig_from_repo_fixture()
    {
        var path = Path.GetFullPath(Path.Combine("data", "maps", "arena_alpha.mapconfig"));
        if (!File.Exists(path))
            return;

        var json = await File.ReadAllTextAsync(path);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        var input = new BrickInput();
        input.Set("map_id", root.GetProperty("map_id").GetString()!);
        input.Set("config", root.GetProperty("config"));

        var brick = new MapFlowBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<MapFlowBrick>.Instance);
        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<string>("audit_id").Should().NotBeNullOrWhiteSpace();
        output.Get<double>("choke_density").Should().BeInRange(0, 1);
        output.Get<double>("spawn_equity").Should().BeInRange(0, 1);
    }
}
