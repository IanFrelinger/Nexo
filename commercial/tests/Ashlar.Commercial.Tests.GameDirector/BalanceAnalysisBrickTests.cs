using FluentAssertions;
using GameDirector.Bricks;
using GameDirector.Bricks.Schemas;
using GameDirector.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Execution;
using Xunit;

namespace Ashlar.Tests.GameDirector;

/// <summary>Tests for balance analysis brick.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class BalanceAnalysisBrickTests
{
    [Fact]
    public void Brick_contract_has_required_inputs_and_outputs()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        brick.Id.Should().Be("balance.analysis");
        brick.Interface.Inputs.Should().Contain(i => i.Name == "sheet_id" && i.Required);
        brick.Interface.Inputs.Should().Contain(i => i.Name == "data" && i.Required);
        brick.Interface.Outputs.Should().Contain(o => o.Name == "audit_id");
        brick.Interface.Outputs.Should().Contain(o => o.Name == "ttk_spread");
        brick.Implementations.Deterministic.Should().NotBeNull();
        brick.Implementations.Agentic.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_with_drift_flags_assault_rifle_ttk()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var input = GameDirectorTestHost.BalanceInput(
            "drift_test",
            ("assault_rifle", new Dictionary<string, double> { ["ttk_ms"] = 1200, ["damage"] = 12, ["fire_rate"] = 600 }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        var flagged = output.Get<List<Outlier>>("flagged");
        flagged.Should().Contain(o => o.Id == "assault_rifle" && o.Stat == "ttk_ms");
        output.Get<double>("ttk_spread").Should().Be(0);
    }

    [Fact]
    public async Task Execute_with_balanced_stats_returns_no_outliers()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var input = GameDirectorTestHost.BalanceInput(
            "stable",
            ("assault_rifle", new Dictionary<string, double> { ["ttk_ms"] = 800, ["damage"] = 12, ["fire_rate"] = 600 }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<List<Outlier>>("flagged").Should().BeEmpty();
        output.Get<List<Delta>>("suggestions").Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_multiple_weapons_computes_ttk_spread_when_two_ttk_outliers()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var input = GameDirectorTestHost.BalanceInput(
            "multi",
            ("assault_rifle", new Dictionary<string, double> { ["ttk_ms"] = 1200 }),
            ("smg", new Dictionary<string, double> { ["ttk_ms"] = 500 }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<List<Outlier>>("flagged").Should().HaveCountGreaterThanOrEqualTo(2);
        output.Get<double>("ttk_spread").Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Execute_empty_data_returns_zero_outliers_but_audit_id()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var input = new BrickInput();
        input.Set("sheet_id", "empty");
        input.Set("data", System.Text.Json.JsonSerializer.SerializeToElement(Array.Empty<object>()));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());

        output.Get<string>("audit_id").Should().NotBeNullOrWhiteSpace();
        output.Get<List<Outlier>>("flagged").Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_writes_audit_log_entry()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var brick = new BalanceAnalysisBrick(audit, NullLogger<BalanceAnalysisBrick>.Instance);
        var input = GameDirectorTestHost.BalanceInput("audit_sheet", ("w1", new Dictionary<string, double> { ["damage"] = 99 }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        var auditId = output.Get<string>("audit_id");

        var records = AuditRecordParser.Query(
            audit,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(1),
            tool: "analyze_balance");
        records.Should().Contain(r => r.AuditId == auditId);
    }

    [Fact]
    public async Task Execute_hybrid_uses_model_for_rationale_when_agentic()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var model = new GameDirectorTestHost.CapturingModel();
        var brick = new BalanceAnalysisBrick(audit, NullLogger<BalanceAnalysisBrick>.Instance, model);
        var input = GameDirectorTestHost.BalanceInput(
            "hybrid",
            ("battle_rifle", new Dictionary<string, double> { ["damage"] = 50 }));

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());

        model.Prompts.Should().NotBeEmpty();
        output.Get<List<Delta>>("suggestions").First().Rationale.Should().Contain("captured-model-output");
    }

    [Fact]
    public async Task Wire_format_round_trip_via_serializer()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
        var brick = registry.GetBrick("balance.analysis")!;

        var wire = new Dictionary<string, object>
        {
            ["sheet_id"] = "wire",
            ["data"] = System.Text.Json.JsonSerializer.SerializeToElement(new[]
            {
                new { id = "smg", stats = new { ttk_ms = 2000.0 } }
            })
        };
        var input = BrickValueSerializer.FromWireToBrickInput(wire);
        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        var back = BrickValueSerializer.ToWireDictionary(output);

        back.Should().ContainKey("audit_id");
        back.Should().ContainKey("flagged");
    }

    [Fact]
    public void Missing_sheet_id_throws()
    {
        var brick = new BalanceAnalysisBrick(GameDirectorTestHost.CreateAuditLog(), NullLogger<BalanceAnalysisBrick>.Instance);
        var input = new BrickInput();
        input.Set("data", System.Text.Json.JsonSerializer.SerializeToElement(Array.Empty<object>()));

        var act = () => brick.ExecuteAsync(input, ImplementationType.Deterministic, GameDirectorTestHost.CreateContext());
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

}
