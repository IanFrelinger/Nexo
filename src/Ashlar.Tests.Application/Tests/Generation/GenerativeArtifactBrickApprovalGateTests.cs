using FluentAssertions;
using Ashlar.Agents.TestKit;
using Ashlar.Core.Application.Generation;
using Ashlar.Core.Application.Orchestration;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Bricks.Ports;
using Ashlar.Core.Domain.Execution;
using Xunit;

namespace Ashlar.Tests.Application.Tests.Generation;

/// <summary>
/// The deploy gate: which artifacts reach <see cref="IDeploymentTarget"/> and why.
///
/// The engine's rule is deny-by-default — a draft that requires human review is
/// deployed ONLY when a configured <see cref="IApprovalGate"/> says a human
/// approved it. These tests pin the whole matrix (approved / denied / no gate)
/// against the recorded gate calls AND the recorded deploy calls, so a
/// regression that silently deploys an unreviewed artifact, or silently
/// withholds an approved one, fails here.
/// </summary>
public sealed class GenerativeArtifactBrickApprovalGateTests
{
    // ---------------------------------------------------------------- approved

    [Fact]
    public async Task ModelDraft_Approved_Deploys_AndReportsHumanApproved()
    {
        var gate = new RecordingApprovalGate(ApprovalResult.Approved);
        var (target, files) = Target();
        var brick = Brick(ModelProfile(target), gate);

        var output = await Run(brick);

        // The gate was actually consulted...
        gate.Requests.Should().HaveCount(1);
        // ...and approval converted "requires review" into a real deploy.
        target.Applied.Should().HaveCount(1, "an approved artifact must reach the deployment target");
        output.Get<bool>("humanApproved").Should().BeTrue();

        var deployment = output.Get<DeploymentApplyResult>("deploymentResult");
        deployment.Succeeded.Should().BeTrue(
            "a human approved this deploy; acceptance must not re-reject it on the same "
            + "human-review ground the gate just cleared");
        files.Applies.Should().HaveCount(1, "the artifact must actually have been written");

        var verdict = target.Verdicts.Single();
        verdict.Decision.Should().Be(AcceptanceDecision.Ship);
        verdict.Signals.Should().Contain("human-approved",
            "the shipped verdict must record that a human — not the evaluator — cleared it");
    }

    [Fact]
    public async Task ModelDraft_Approved_StillRollsBack_WhenSmokeFails()
    {
        // Approval clears the human-review ground and NOTHING else: a human saying
        // "yes, ship it" must not overrule a deployment that then fails its smoke.
        var gate = new RecordingApprovalGate(ApprovalResult.Approved);
        var files = new FakeManagedFileSet();
        var target = new FakeDeploymentTarget(
            files,
            Scripted<DeploymentSmokeResult>.Always(DeploymentSmokeResult.Fail("smoke blew up")));
        var brick = Brick(ModelProfile(target), gate);

        var output = await Run(brick);

        output.Get<bool>("humanApproved").Should().BeTrue();
        target.Applied.Should().HaveCount(1, "approval did let it reach the target");

        var deployment = output.Get<DeploymentApplyResult>("deploymentResult");
        deployment.Succeeded.Should().BeFalse("evidence still outranks approval");
        deployment.Message.Should().Contain("smoke", "the smoke failure is the real reason");
        target.Verdicts.Single().Decision.Should().Be(AcceptanceDecision.Rollback);
        target.PriorImageRestored.Should().BeTrue("a failed smoke must still roll back");
    }

    [Fact]
    public async Task ApprovalRequest_CarriesTargetStrategyAndTheEngineTimeout()
    {
        var gate = new RecordingApprovalGate(ApprovalResult.Approved);
        var (target, _) = Target();
        var brick = Brick(ModelProfile(target), gate);

        await Run(brick);

        var request = gate.Requests.Single();
        request.Description.Should().Contain("deploy-demo", "a human must know what they are approving");
        request.Description.Should().Contain("Model", "…and that it came from the model path");
        request.Timeout.Should().Be(AcceptanceRouting.DefaultApprovalTimeout);
    }

    // ------------------------------------------------------------------ denied

    [Fact]
    public async Task ModelDraft_Denied_WithholdsDeployment_AndExplainsWhy()
    {
        var gate = new RecordingApprovalGate(ApprovalResult.Denied);
        var (target, files) = Target();
        var brick = Brick(ModelProfile(target), gate);

        var output = await Run(brick);

        gate.Requests.Should().HaveCount(1);
        target.Applied.Should().BeEmpty("a denied artifact must never reach the deployment target");
        files.Applies.Should().BeEmpty("nothing may be written when review was denied");
        output.Get<bool>("humanApproved").Should().BeFalse();

        var deployment = output.Get<DeploymentApplyResult>("deploymentResult");
        deployment.Succeeded.Should().BeFalse();
        deployment.Message.Should().Contain("withheld", "the withhold must be explained, not silent");
        deployment.Message.Should().Contain("Denied", "…including which verdict caused it");
    }

    [Fact]
    public async Task ModelDraft_TimedOutReview_WithholdsDeployment()
    {
        var gate = new RecordingApprovalGate(ApprovalResult.TimedOut);
        var (target, _) = Target();
        var brick = Brick(ModelProfile(target), gate);

        var output = await Run(brick);

        target.Applied.Should().BeEmpty("a timed-out review is not an approval");
        output.Get<bool>("humanApproved").Should().BeFalse();
        output.Get<DeploymentApplyResult>("deploymentResult").Message.Should().Contain("TimedOut");
    }

    // ---------------------------------------------------------------- no gate

    [Fact]
    public async Task ModelDraft_WithNoGateConfigured_WithholdsDeployment_DenyByDefault()
    {
        var (target, files) = Target();
        // No approval gate supplied at all — the host never wired one up.
        var brick = Brick(ModelProfile(target), approvalGate: null);

        var output = await Run(brick);

        target.Applied.Should().BeEmpty(
            "with no gate there is no human in the loop, so a review-required artifact "
            + "must be withheld rather than deployed unreviewed");
        files.Applies.Should().BeEmpty();
        output.Get<bool>("humanApproved").Should().BeFalse();
        output.Get<DeploymentApplyResult>("deploymentResult").Succeeded.Should().BeFalse();
    }

    // -------------------------------------------------- the tunable itself

    [Fact]
    public async Task RequireHumanReviewForModelDrafts_DefaultsToTrue_AndTriggersReviewOnASuccessfulDraft()
    {
        // Nothing sets the tunable: this asserts the DEFAULT is the safe one.
        new GenerationTunables().RequireHumanReviewForModelDrafts.Should().BeTrue();

        var gate = new RecordingApprovalGate(ApprovalResult.Approved);
        var (target, _) = Target();
        var brick = Brick(ModelProfile(target), gate);

        var output = await Run(brick);

        // The draft passed every validator — review is required by POLICY, not failure.
        output.Get<bool>("verified").Should().BeTrue();
        output.Get<GenerativeProvenance>(GenerativeBrick.ProvenanceOutputKey)
            .RequiresHumanReview.Should().BeTrue();
        gate.Requests.Should().HaveCount(1,
            "a clean model draft still requires review while the tunable defaults to true");
    }

    [Fact]
    public async Task ModelDraft_WithReviewTunableDisabled_DeploysWithoutConsultingAGate()
    {
        var gate = new RecordingApprovalGate(ApprovalResult.Denied);
        var (target, _) = Target();
        var profile = ModelProfile(target, new GenerationTunables
        {
            PreferDeterministic = false,
            RequireHumanReviewForModelDrafts = false
        });
        var brick = Brick(profile, gate);

        var output = await Run(brick);

        gate.WasNeverConsulted.Should().BeTrue(
            "a profile that opted out of model-draft review has no review to route");
        target.Applied.Should().HaveCount(1);
        Has(output, "humanApproved").Should().BeFalse(
            "no review happened, so there is no human verdict to report");
        output.Get<DeploymentApplyResult>("deploymentResult").Succeeded.Should().BeTrue();
    }

    // ------------------------------------------------------- deterministic

    [Fact]
    public async Task DeterministicDraft_RequiresNoReview_AndDeploysWithNoGateAtAll()
    {
        var (target, files) = Target();
        var profile = new AgentProfile
        {
            TargetId = "deploy-demo",
            Drafter = new FixedDrafter("MODEL PATH MUST NOT RUN"),
            DeterministicDrafter = new FixedDeterministic("SELECT 1"),
            Tunables = new GenerationTunables { PreferDeterministic = true },
            Capabilities = new AgentProfileCapabilities
            {
                SupportsDeterministic = true,
                SupportsDeployment = true
            },
            Validators = new object[] { new AlwaysOkValidator() },
            Deployment = target
        };
        var brick = Brick(profile, approvalGate: null);

        var output = await Run(brick);

        var provenance = output.Get<GenerativeProvenance>(GenerativeBrick.ProvenanceOutputKey);
        provenance.Strategy.Should().Be(GenerationStrategy.Deterministic);
        provenance.RequiresHumanReview.Should().BeFalse(
            "a deterministic draft is reproducible evidence, not a model's guess");

        target.Applied.Should().HaveCount(1,
            "no review required means no gate required — this must deploy unattended");
        files.Applies.Should().HaveCount(1);
        Has(output, "humanApproved").Should().BeFalse();
        output.Get<DeploymentApplyResult>("deploymentResult").Succeeded.Should().BeTrue();
    }

    // ------------------------------------------------------------- helpers

    private static (FakeDeploymentTarget Target, FakeManagedFileSet Files) Target()
    {
        var files = new FakeManagedFileSet();
        var target = new FakeDeploymentTarget(
            files,
            Scripted<DeploymentSmokeResult>.Always(DeploymentSmokeResult.Pass("smoke ok")));
        return (target, files);
    }

    /// <summary>True when the brick emitted the named output at all.</summary>
    private static bool Has(BrickOutput output, string key) =>
        output.ToDictionary().ContainsKey(key);

    private static AgentProfile ModelProfile(IDeploymentTarget target, GenerationTunables? tunables = null) => new()
    {
        TargetId = "deploy-demo",
        Drafter = new FixedDrafter("DRAFTED"),
        Tunables = tunables ?? new GenerationTunables { PreferDeterministic = false },
        Capabilities = new AgentProfileCapabilities { SupportsDeployment = true },
        Validators = new object[] { new AlwaysOkValidator() },
        Deployment = target
    };

    private static GenerativeArtifactBrick Brick(AgentProfile profile, IApprovalGate? approvalGate)
    {
        var registry = new AgentProfileRegistry();
        registry.Register(profile);
        return new GenerativeArtifactBrick(registry, logger: null, resilience: null, approvalGate: approvalGate);
    }

    private static Task<BrickOutput> Run(GenerativeArtifactBrick brick)
    {
        var input = new BrickInput();
        input.Set("target", "deploy-demo");
        input.Set("grounding", "events");
        return brick.ExecuteAsync(input, ImplementationType.Agentic, new FakeContext(), CancellationToken.None);
    }

    private sealed class FixedDrafter : IArtifactDrafter
    {
        private readonly string _content;
        public FixedDrafter(string content) => _content = content;
        public Task<GeneratedArtifact> DraftAsync(
            GenerationRequest request,
            IReadOnlyList<string> priorFeedback,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new GeneratedArtifact { Content = _content });
    }

    private sealed class FixedDeterministic : IDeterministicDrafter
    {
        private readonly string _content;
        public FixedDeterministic(string content) => _content = content;
        public bool TryDraft(GenerationRequest request, out GeneratedArtifact? artifact)
        {
            artifact = new GeneratedArtifact { Content = _content };
            return true;
        }
    }

    private sealed class AlwaysOkValidator : IPostValidator<GeneratedArtifact>
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(
            GeneratedArtifact output, CancellationToken ct) =>
            new((true, (string?)null));
    }

    private sealed class FakeContext : IExecutionContext
    {
        public string AgentId => "t";
        public string BehaviorId => "t";
        public bool IsAirGapped => false;
        public bool AuditMode => false;
        public string Provider => "mock";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
