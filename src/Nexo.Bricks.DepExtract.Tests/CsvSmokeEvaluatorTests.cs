using FluentAssertions;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Part (b): the C++ profile's acceptance evaluator. Pure over captured CSV —
/// row-count sanity plus field/shape checks. Retires the rows&gt;=1 oracle.
/// </summary>
public sealed class CsvSmokeEvaluatorTests
{
    private const string GoodCsv =
        "SystemTime,EventId,Payload\n" +
        "2024-01-01T00:00:00Z,1,a\n" +
        "2024-01-01T00:00:01Z,2,b\n" +
        "2024-01-01T00:00:02Z,3,c\n" +
        "2024-01-01T00:00:03Z,4,d\n" +
        "2024-01-01T00:00:04Z,5,e\n" +
        "2024-01-01T00:00:05Z,6,f\n";

    [Fact]
    public void One_row_is_not_acceptance_anymore()
    {
        var csv = "SystemTime,EventId\n2024-01-01T00:00:00Z,1\n";

        var verdict = Evaluate(new CsvSmokeEvaluator(), csv);

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Reason.Should().Contain("1");
        verdict.Signals.Should().Contain(s => s.Contains("row-floor"));
    }

    [Fact]
    public void Rows_at_or_above_the_floor_ship()
    {
        var verdict = Evaluate(new CsvSmokeEvaluator(), GoodCsv);

        verdict.Decision.Should().Be(AcceptanceDecision.Ship);
    }

    [Fact]
    public void Default_row_floor_is_well_above_one()
    {
        new CsvSmokeEvaluatorOptions().MinRows.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void Header_only_csv_rolls_back()
    {
        var verdict = Evaluate(new CsvSmokeEvaluator(), "SystemTime,EventId\n");

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
    }

    [Fact]
    public void Ragged_column_count_rolls_back()
    {
        var csv = GoodCsv + "2024-01-01T00:00:06Z,7\n"; // missing third column

        var verdict = Evaluate(new CsvSmokeEvaluator(), csv);

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Signals.Should().Contain(s => s.Contains("column"));
    }

    [Fact]
    public void Non_monotonic_timestamps_roll_back()
    {
        var csv =
            "SystemTime,EventId,Payload\n" +
            "2024-01-01T00:00:05Z,1,a\n" +
            "2024-01-01T00:00:04Z,2,b\n" +
            "2024-01-01T00:00:03Z,3,c\n" +
            "2024-01-01T00:00:02Z,4,d\n" +
            "2024-01-01T00:00:01Z,5,e\n" +
            "2024-01-01T00:00:00Z,6,f\n";

        var verdict = Evaluate(new CsvSmokeEvaluator(), csv);

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Signals.Should().Contain(s => s.Contains("monotonic"));
    }

    [Fact]
    public void Required_columns_are_enforced()
    {
        var evaluator = new CsvSmokeEvaluator(new CsvSmokeEvaluatorOptions
        {
            RequiredColumns = new[] { "SystemTime", "Provider" }
        });

        var verdict = Evaluate(evaluator, GoodCsv);

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Reason.Should().Contain("Provider");
    }

    [Fact]
    public void Every_captured_sample_must_clear_the_bar()
    {
        // Multi-sample agreement falls out of Outputs: one weak sample sinks it.
        var smoke = DeploymentSmokeResult.Pass("ok", new Dictionary<string, string>
        {
            ["sample-a"] = GoodCsv,
            ["sample-b"] = "SystemTime,EventId,Payload\n2024-01-01T00:00:00Z,1,a\n"
        });

        var verdict = new CsvSmokeEvaluator().Evaluate(Context(smoke));

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Reason.Should().Contain("sample-b");
    }

    [Fact]
    public void All_samples_strong_ships()
    {
        var smoke = DeploymentSmokeResult.Pass("ok", new Dictionary<string, string>
        {
            ["sample-a"] = GoodCsv,
            ["sample-b"] = GoodCsv
        });

        new CsvSmokeEvaluator().Evaluate(Context(smoke)).Decision
            .Should().Be(AcceptanceDecision.Ship);
    }

    [Fact]
    public void Failed_smoke_transport_rolls_back_without_inspecting_output()
    {
        var smoke = DeploymentSmokeResult.Fail("extract HTTP 500");

        var verdict = new CsvSmokeEvaluator().Evaluate(Context(smoke));

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Reason.Should().Contain("HTTP 500");
    }

    [Fact]
    public void Row_count_in_smoke_detail_is_used_when_no_csv_was_captured()
    {
        var smoke = DeploymentSmokeResult.Pass("health+extract ok rows=2 job=j1");

        var verdict = new CsvSmokeEvaluator().Evaluate(Context(smoke));

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Signals.Should().Contain(s => s.Contains("row-floor"));
    }

    [Fact]
    public void Health_only_smoke_is_reported_as_such_not_silently_accepted()
    {
        var smoke = DeploymentSmokeResult.Pass("health ok");

        var verdict = new CsvSmokeEvaluator().Evaluate(Context(smoke));

        verdict.Decision.Should().Be(AcceptanceDecision.Ship);
        verdict.Signals.Should().Contain("health-only");
    }

    [Fact]
    public void Evaluator_is_pure_and_repeatable()
    {
        var evaluator = new CsvSmokeEvaluator();
        var ctx = Context(DeploymentSmokeResult.Pass("ok", new Dictionary<string, string> { ["s"] = GoodCsv }));

        var first = evaluator.Evaluate(ctx);
        var second = evaluator.Evaluate(ctx);

        second.Decision.Should().Be(first.Decision);
        second.Reason.Should().Be(first.Reason);
    }

    private static AcceptanceResult Evaluate(CsvSmokeEvaluator evaluator, string csv) =>
        evaluator.Evaluate(Context(DeploymentSmokeResult.Pass(
            "ok",
            new Dictionary<string, string> { ["sample"] = csv })));

    private static AcceptanceContext Context(DeploymentSmokeResult smoke) =>
        new(
            new GeneratedArtifact { Content = "#pragma once\n" },
            GenerativeProvenance.Create(GenerationStrategy.Templated, verified: true),
            smoke);
}
