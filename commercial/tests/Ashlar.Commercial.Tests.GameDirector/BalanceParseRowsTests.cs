using System.Text.Json;
using FluentAssertions;
using GameDirector.Bricks;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Xunit;

namespace Ashlar.Tests.GameDirector;

/// <summary>Tests for balance parse rows.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class BalanceParseRowsTests
{
    /// <summary>New brick.</summary>
    private static BalanceAnalysisBrick NewBrick() => new(
        GameDirectorTestHost.CreateAuditLog(),
        NullLogger<BalanceAnalysisBrick>.Instance);

    private static async Task<int> FlaggedCountAsync(BrickInput input)
    {
        var output = await NewBrick().ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        var flagged = output.Get<List<global::GameDirector.Bricks.Schemas.Outlier>>("flagged");
        return flagged.Count;
    }

    [Fact]
    public async Task ParseRows_accepts_json_string()
    {
        var input = new BrickInput();
        input.Set("sheet_id", "s");
        input.Set("data", "[{\"id\":\"assault_rifle\",\"stats\":{\"damage\":99}}]");
        (await FlaggedCountAsync(input)).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ParseRows_accepts_JsonElement_string()
    {
        var input = new BrickInput();
        input.Set("sheet_id", "s");
        input.Set("data", JsonDocument.Parse("\"[{\\\"id\\\":\\\"assault_rifle\\\",\\\"stats\\\":{\\\"damage\\\":99}}]\"").RootElement);
        (await FlaggedCountAsync(input)).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ParseRows_accepts_dictionary_enumeration_with_jsonelements()
    {
        var rows = new List<object>
        {
            JsonDocument.Parse("{\"id\":\"assault_rifle\",\"stats\":{\"damage\":99}}").RootElement
        };
        var input = new BrickInput();
        input.Set("sheet_id", "s");
        input.Set("data", rows);
        (await FlaggedCountAsync(input)).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ParseRows_accepts_dictionary_enumeration_with_dictionaries()
    {
        var rows = new List<object>
        {
            new Dictionary<string, object>
            {
                ["id"] = "assault_rifle",
                ["stats"] = new Dictionary<string, object>
                {
                    ["damage"] = 99.0,
                    ["fire_rate"] = "1200",
                    ["ttk_ms"] = 1500L,
                    ["bogus"] = "not-a-number"
                }
            }
        };
        var input = new BrickInput();
        input.Set("sheet_id", "s");
        input.Set("data", rows);
        (await FlaggedCountAsync(input)).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ParseRows_dictionary_skips_row_without_id()
    {
        var rows = new List<object>
        {
            new Dictionary<string, object> { ["stats"] = new Dictionary<string, object>() }
        };
        var input = new BrickInput();
        input.Set("sheet_id", "s");
        input.Set("data", rows);
        (await FlaggedCountAsync(input)).Should().Be(0);
    }

    [Fact]
    public async Task ParseRows_returns_empty_when_data_missing()
    {
        var input = new BrickInput();
        input.Set("sheet_id", "s");
        (await FlaggedCountAsync(input)).Should().Be(0);
    }

    [Fact]
    public async Task ParseRows_returns_empty_when_data_unsupported_type()
    {
        var input = new BrickInput();
        input.Set("sheet_id", "s");
        input.Set("data", 42);
        (await FlaggedCountAsync(input)).Should().Be(0);
    }

    [Fact]
    public async Task ParseRows_returns_empty_when_json_string_is_not_array()
    {
        var input = new BrickInput();
        input.Set("sheet_id", "s");
        input.Set("data", "{\"id\":\"x\"}");
        (await FlaggedCountAsync(input)).Should().Be(0);
    }

    [Fact]
    public async Task ParseRows_returns_empty_when_json_string_is_malformed()
    {
        var input = new BrickInput();
        input.Set("sheet_id", "s");
        input.Set("data", "[not json");
        (await FlaggedCountAsync(input)).Should().Be(0);
    }

    [Fact]
    public async Task ParseRows_returns_empty_when_json_string_is_blank()
    {
        var input = new BrickInput();
        input.Set("sheet_id", "s");
        input.Set("data", "   ");
        (await FlaggedCountAsync(input)).Should().Be(0);
    }
}
