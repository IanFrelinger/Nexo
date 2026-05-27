using System.Text.Json;
using FluentAssertions;
using GameDirector.Bricks;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.GameDirector;

[Trait("Category", "GameDirectorApplication")]
public sealed class MapFlowParseConfigTests
{
    private static MapFlowBrick NewBrick() => new(
        GameDirectorTestHost.CreateAuditLog(),
        NullLogger<MapFlowBrick>.Instance);

    [Fact]
    public async Task ParseConfig_accepts_string_config()
    {
        var brick = NewBrick();
        var input = new BrickInput();
        input.Set("map_id", "m");
        input.Set("config", "{\"tiles\":[[{\"x\":0,\"y\":0,\"traversalOptions\":1}]]," +
            "\"spawns\":[{\"id\":\"s1\",\"x\":0,\"y\":0},{\"id\":\"s2\",\"x\":50,\"y\":50}]}");

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<double>("spawn_equity").Should().BeInRange(0, 1);
    }

    [Fact]
    public async Task ParseConfig_accepts_string_encoded_tiles_and_spawns()
    {
        var brick = NewBrick();
        var input = new BrickInput();
        input.Set("map_id", "m");
        input.Set("config", JsonSerializer.SerializeToElement(new
        {
            tiles = "[[{\"x\":0,\"y\":0,\"traversalOptions\":1}]]",
            spawns = "[{\"id\":\"s1\",\"x\":0,\"y\":0}]"
        }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<double>("choke_density").Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ParseConfig_returns_empty_when_config_missing()
    {
        var brick = NewBrick();
        var input = new BrickInput();
        input.Set("map_id", "m");

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<double>("choke_density").Should().Be(0);
        output.Get<double>("spawn_equity").Should().Be(1.0);
    }

    [Fact]
    public async Task ParseConfig_accepts_dictionary_when_string_blank()
    {
        var brick = NewBrick();
        var input = new BrickInput();
        input.Set("map_id", "m");
        var dict = new Dictionary<string, object> { ["tiles"] = Array.Empty<object>(), ["spawns"] = Array.Empty<object>() };
        input.Set("config", dict);

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<double>("choke_density").Should().Be(0);
    }

    [Fact]
    public async Task Gini_handles_zero_sum_spawns()
    {
        var brick = NewBrick();
        var input = new BrickInput();
        input.Set("map_id", "m");
        input.Set("config", JsonSerializer.SerializeToElement(new
        {
            tiles = Array.Empty<object>(),
            spawns = new[] { new { id = "a", x = 0, y = 0 }, new { id = "b", x = 0, y = 0 } }
        }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<double>("spawn_equity").Should().Be(1.0);
    }
}
