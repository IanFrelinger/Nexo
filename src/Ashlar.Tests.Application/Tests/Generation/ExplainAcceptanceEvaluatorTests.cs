using FluentAssertions;
using Ashlar.Bricks.SqlProfile;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Bricks.Ports;
using Xunit;

namespace Ashlar.Tests.Application.Tests.Generation;

/// <summary>
/// Part (b): the SQL profile's acceptance evaluator — proves the port generalizes
/// to a domain with no container, no image, and no file set.
/// </summary>
public sealed class ExplainAcceptanceEvaluatorTests
{
    private const string GoodPlan =
        "QUERY PLAN\n" +
        "SEARCH TABLE orders USING INDEX idx_orders_customer (customer_id=?)\n";

    [Fact]
    public void Clean_plan_ships()
    {
        Evaluate(new ExplainAcceptanceEvaluator(), GoodPlan)
            .Decision.Should().Be(AcceptanceDecision.Ship);
    }

    [Fact]
    public void Empty_plan_rolls_back()
    {
        var verdict = Evaluate(new ExplainAcceptanceEvaluator(), "   ");

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Signals.Should().Contain("empty-plan");
    }

    [Fact]
    public void Parse_error_in_plan_rolls_back()
    {
        var verdict = Evaluate(
            new ExplainAcceptanceEvaluator(),
            "ERROR:  syntax error at or near \"FROM\"");

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Signals.Should().Contain("plan-error");
    }

    [Fact]
    public void Missing_relation_rolls_back()
    {
        var verdict = Evaluate(new ExplainAcceptanceEvaluator(), "no such table: orders");

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Signals.Should().Contain("plan-error");
    }

    [Fact]
    public void Full_scan_is_allowed_by_default_and_rejectable_by_option()
    {
        const string scanPlan = "QUERY PLAN\nSCAN TABLE orders\n";

        Evaluate(new ExplainAcceptanceEvaluator(), scanPlan)
            .Decision.Should().Be(AcceptanceDecision.Ship);

        var strict = new ExplainAcceptanceEvaluator(new ExplainAcceptanceEvaluatorOptions
        {
            RejectFullTableScan = true
        });

        var verdict = Evaluate(strict, scanPlan);
        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Signals.Should().Contain("full-table-scan");
    }

    [Fact]
    public void Every_captured_plan_must_be_clean()
    {
        var smoke = DeploymentSmokeResult.Pass("ok", new Dictionary<string, string>
        {
            ["q1"] = GoodPlan,
            ["q2"] = "ERROR:  syntax error"
        });

        var verdict = new ExplainAcceptanceEvaluator().Evaluate(Context(smoke));

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Reason.Should().Contain("q2");
    }

    [Fact]
    public void Failed_smoke_rolls_back()
    {
        var verdict = new ExplainAcceptanceEvaluator()
            .Evaluate(Context(DeploymentSmokeResult.Fail("connection refused")));

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Reason.Should().Contain("connection refused");
    }

    [Fact]
    public void No_plan_captured_is_reported_not_silently_accepted()
    {
        var verdict = new ExplainAcceptanceEvaluator()
            .Evaluate(Context(DeploymentSmokeResult.Pass("statement prepared")));

        verdict.Decision.Should().Be(AcceptanceDecision.Ship);
        verdict.Signals.Should().Contain("no-plan-captured");
    }

    [Fact]
    public void Evaluator_performs_no_io()
    {
        // A pure function of its context: same input, same verdict, no ambient state.
        var evaluator = new ExplainAcceptanceEvaluator();
        var ctx = Context(DeploymentSmokeResult.Pass("ok", new Dictionary<string, string> { ["q"] = GoodPlan }));

        evaluator.Evaluate(ctx).Reason.Should().Be(evaluator.Evaluate(ctx).Reason);
    }

    private static AcceptanceResult Evaluate(ExplainAcceptanceEvaluator evaluator, string plan) =>
        evaluator.Evaluate(Context(DeploymentSmokeResult.Pass(
            "ok",
            new Dictionary<string, string> { ["plan"] = plan })));

    private static AcceptanceContext Context(DeploymentSmokeResult smoke) =>
        new(
            new GeneratedArtifact { Content = "SELECT 1" },
            GenerativeProvenance.Create(GenerationStrategy.Templated, verified: true),
            smoke);
}
