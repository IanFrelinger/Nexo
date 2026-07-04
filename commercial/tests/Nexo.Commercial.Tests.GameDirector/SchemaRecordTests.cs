using FluentAssertions;
using GameDirector.Bricks.Schemas;
using Xunit;

namespace Nexo.Tests.GameDirector;

/// <summary>Tests for schema record.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class SchemaRecordTests
{
    [Fact]
    public void Balance_records_round_trip_values()
    {
        var stats = new Dictionary<string, double> { ["damage"] = 12 };
        var row = new StatRow("ar", stats);
        var outlier = new Outlier("ar", "damage", 15, 12, 25);
        var delta = new Delta("ar", "damage", 12, "Reduce by 25%");
        var input = new BalanceAnalysisInput("sheet-1", [row], "baseline-1");
        var output = new BalanceAnalysisOutput("audit-1", [outlier], 50.5, [delta]);

        row.Id.Should().Be("ar");
        row.Stats["damage"].Should().Be(12);
        outlier.Id.Should().Be("ar");
        outlier.Stat.Should().Be("damage");
        outlier.Value.Should().Be(15);
        outlier.BaselineValue.Should().Be(12);
        outlier.DeltaPct.Should().Be(25);
        delta.Id.Should().Be("ar");
        delta.Stat.Should().Be("damage");
        delta.SuggestedValue.Should().Be(12);
        delta.Rationale.Should().Contain("Reduce");
        input.SheetId.Should().Be("sheet-1");
        input.Data.Should().ContainSingle();
        input.BaselineId.Should().Be("baseline-1");
        output.AuditId.Should().Be("audit-1");
        output.Flagged.Should().ContainSingle();
        output.Suggestions.Should().ContainSingle();
        output.TtkSpread.Should().Be(50.5);
    }

    [Fact]
    public void Map_records_round_trip_values()
    {
        var tile = new TileRow(1, 2, 4);
        var spawn = new SpawnPoint("s1", 10, 20);
        var cfg = new MapConfig([[tile]], [spawn], new Dictionary<string, object> { ["mode"] = "ctf" });
        var input = new MapFlowInput("map-1", cfg, "sess-1");
        var output = new MapFlowOutput("a1", 0.5, 0.9, "report", ["rec"]);

        tile.X.Should().Be(1);
        tile.Y.Should().Be(2);
        tile.TraversalOptions.Should().Be(4);
        spawn.Id.Should().Be("s1");
        spawn.X.Should().Be(10);
        spawn.Y.Should().Be(20);
        cfg.Tiles.Should().HaveCount(1);
        cfg.Spawns.Should().HaveCount(1);
        cfg.Metadata!["mode"].Should().Be("ctf");
        input.MapId.Should().Be("map-1");
        input.Config.Should().BeSameAs(cfg);
        input.SessionId.Should().Be("sess-1");
        output.AuditId.Should().Be("a1");
        output.ChokeDensity.Should().Be(0.5);
        output.SpawnEquity.Should().Be(0.9);
        output.SightlineReport.Should().Be("report");
        output.Recommendations.Should().HaveCount(1);
    }

    [Fact]
    public void Content_records_round_trip_values()
    {
        var input = new ContentGenerationInput("sess-1", "prompt", "flavor", Fast: true);
        var output = new ContentGenerationOutput("a-1", "text", "model", 42);

        input.Fast.Should().BeTrue();
        input.Prompt.Should().Be("prompt");
        input.TargetType.Should().Be("flavor");
        input.SessionId.Should().Be("sess-1");
        output.AuditId.Should().Be("a-1");
        output.Content.Should().Be("text");
        output.ModelUsed.Should().Be("model");
        output.Tokens.Should().Be(42);
    }
}
