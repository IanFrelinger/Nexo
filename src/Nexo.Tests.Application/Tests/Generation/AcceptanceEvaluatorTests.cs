using FluentAssertions;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Xunit;

namespace Nexo.Tests.Application.Tests.Generation;

/// <summary>
/// Part (a): the conservative built-in acceptance evaluator, and the routing of
/// its human-review signal onto the existing IApprovalGate port.
/// </summary>
public sealed class AcceptanceEvaluatorTests
{
    [Fact]
    public void Default_ships_when_smoke_passed_and_provenance_clean()
    {
        var verdict = DefaultAcceptanceEvaluator.Instance.Evaluate(
            Context(DeploymentSmokeResult.Pass("all good"), Verified()));

        verdict.Decision.Should().Be(AcceptanceDecision.Ship);
        verdict.Signals.Should().Contain("smoke-passed");
    }

    [Fact]
    public void Default_ships_when_no_smoke_configured()
    {
        var verdict = DefaultAcceptanceEvaluator.Instance.Evaluate(
            Context(DeploymentSmokeResult.NotRun, Verified()));

        verdict.Decision.Should().Be(AcceptanceDecision.Ship);
        verdict.Signals.Should().Contain("smoke-not-run");
    }

    [Fact]
    public void Default_rolls_back_when_smoke_failed()
    {
        var verdict = DefaultAcceptanceEvaluator.Instance.Evaluate(
            Context(DeploymentSmokeResult.Fail("no output produced"), Verified()));

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Reason.Should().Contain("no output produced");
        verdict.Signals.Should().Contain("smoke-failed");
    }

    [Fact]
    public void Default_rolls_back_when_provenance_unverified()
    {
        var verdict = DefaultAcceptanceEvaluator.Instance.Evaluate(
            Context(
                DeploymentSmokeResult.Pass("ok"),
                GenerativeProvenance.Create(GenerationStrategy.Model, verified: false)));

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Signals.Should().Contain("unverified");
    }

    [Fact]
    public void Default_emits_human_review_signal_rather_than_calling_a_gate()
    {
        var verdict = DefaultAcceptanceEvaluator.Instance.Evaluate(
            Context(
                DeploymentSmokeResult.Pass("ok"),
                GenerativeProvenance.Create(
                    GenerationStrategy.Model, verified: true, requiresHumanReview: true)));

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Signals.Should().Contain(DefaultAcceptanceEvaluator.HumanReviewSignal);
        AcceptanceRouting.RequiresHumanReview(verdict).Should().BeTrue();
    }

    [Fact]
    public async Task Routing_turns_human_review_rollback_into_ship_when_approved()
    {
        var gate = new RecordingApprovalGate(ApprovalResult.Approved);

        var verdict = await AcceptanceRouting.EvaluateAsync(
            evaluator: null,
            Context(
                DeploymentSmokeResult.Pass("ok"),
                GenerativeProvenance.Create(
                    GenerationStrategy.Model, verified: true, requiresHumanReview: true)),
            gate);

        verdict.Decision.Should().Be(AcceptanceDecision.Ship);
        verdict.Signals.Should().Contain("human-approved");
        gate.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task Routing_keeps_rollback_when_denied()
    {
        var gate = new RecordingApprovalGate(ApprovalResult.Denied);

        var verdict = await AcceptanceRouting.EvaluateAsync(
            evaluator: null,
            Context(
                DeploymentSmokeResult.Pass("ok"),
                GenerativeProvenance.Create(
                    GenerationStrategy.Model, verified: true, requiresHumanReview: true)),
            gate);

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        verdict.Signals.Should().Contain("human-denied");
        gate.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task Routing_denies_by_default_when_no_gate_is_registered()
    {
        var verdict = await AcceptanceRouting.EvaluateAsync(
            evaluator: null,
            Context(
                DeploymentSmokeResult.Pass("ok"),
                GenerativeProvenance.Create(
                    GenerationStrategy.Model, verified: true, requiresHumanReview: true)),
            gate: null);

        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
    }

    [Fact]
    public async Task Routing_does_not_consult_the_gate_for_evidence_based_rollbacks()
    {
        var gate = new RecordingApprovalGate(ApprovalResult.Approved);

        var verdict = await AcceptanceRouting.EvaluateAsync(
            evaluator: null,
            Context(DeploymentSmokeResult.Fail("no output produced"), Verified()),
            gate);

        // A human cannot approve away a failed smoke.
        verdict.Decision.Should().Be(AcceptanceDecision.Rollback);
        gate.Requests.Should().BeEmpty();
    }

    private static AcceptanceContext Context(
        DeploymentSmokeResult smoke,
        GenerativeProvenance provenance) =>
        new(new GeneratedArtifact { Content = "artifact" }, provenance, smoke);

    private static GenerativeProvenance Verified() =>
        GenerativeProvenance.Create(GenerationStrategy.Templated, verified: true);

    private sealed class RecordingApprovalGate : IApprovalGate
    {
        private readonly ApprovalResult _result;
        public List<string> Requests { get; } = new();

        public RecordingApprovalGate(ApprovalResult result) => _result = result;

        public Task<ApprovalResult> RequestApprovalAsync(
            string actionDescription,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(actionDescription);
            return Task.FromResult(_result);
        }
    }
}
