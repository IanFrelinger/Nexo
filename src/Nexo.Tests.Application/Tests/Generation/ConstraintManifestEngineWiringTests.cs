using FluentAssertions;
using Nexo.Agents.TestKit;
using Nexo.Core.Application.Generation;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.Application.Tests.Generation;

/// <summary>
/// Engine wiring for constraint manifests (trust-loop R3.5): a profile that declares a
/// manifest gets its instructions injected into the <see cref="GenerationRequest"/> AND
/// the pre-gate enforced ahead of its own validators — without the profile adding the
/// validator itself. No manifest, no gate.
/// </summary>
public sealed class ConstraintManifestEngineWiringTests
{
    private static readonly BrickConstraintManifest Manifest = new()
    {
        ForbiddenApiTokens = new[] { "DateTime.Now" }
    };

    [Fact]
    public async Task ManifestViolation_IsRejectedByThePreGate_AndTheInstructionFeedsTheRepair()
    {
        var drafter = FakeArtifactDrafter.OfContent("var t = DateTime.Now;", "clean artifact");
        var brick = BrickFor(drafter, Manifest);

        var output = await Execute(brick, "manifest-demo");

        drafter.Attempts.Should().Be(2, "the pre-gate must reject the first draft and trigger a repair");
        var secondAttemptFeedback = string.Join(" ", drafter.FeedbackPerAttempt[1]);
        secondAttemptFeedback.Should().Contain(Manifest.ForbiddenApiInstruction("DateTime.Now"),
            "the repair prompt must restate the violated constraint verbatim (R3.4)");
        output.Get<bool>("verified").Should().BeTrue();
    }

    [Fact]
    public async Task ManifestInstructions_TravelOnTheGenerationRequest()
    {
        var drafter = FakeArtifactDrafter.OfContent("clean artifact");
        var brick = BrickFor(drafter, Manifest);

        await Execute(brick, "manifest-demo");

        drafter.Requests.Should().NotBeEmpty();
        drafter.Requests[0].ConstraintInstructions.Should().Contain(
            Manifest.ForbiddenApiInstruction("DateTime.Now"),
            "R3.5 first use: the proposer must be shown the same rules the gate enforces");
    }

    [Fact]
    public async Task WithoutAManifest_NoPreGateRuns_AndInstructionsStayEmpty()
    {
        // The profile carries one always-passing gate: registration requires a gate
        // (validator or manifest), and this test's point is that no MANIFEST rules
        // run — the forbidden token sails through the declared validator untouched.
        var drafter = FakeArtifactDrafter.OfContent("var t = DateTime.Now;");
        var brick = BrickFor(drafter, manifest: null, new AlwaysPassValidator());

        var output = await Execute(brick, "manifest-demo");

        drafter.Attempts.Should().Be(1);
        drafter.Requests[0].ConstraintInstructions.Should().BeEmpty();
        output.Get<bool>("verified").Should().BeTrue("no manifest means no manifest rules are enforced");
    }

    private static GenerativeArtifactBrick BrickFor(
        FakeArtifactDrafter drafter,
        BrickConstraintManifest? manifest,
        params object[] validators)
    {
        var registry = new AgentProfileRegistry();
        registry.Register(new AgentProfile
        {
            TargetId = "manifest-demo",
            Drafter = drafter,
            ConstraintManifest = manifest,
            Tunables = new GenerationTunables
            {
                PreferDeterministic = false,
                MaxRepairAttempts = 3,
                RequireHumanReviewForModelDrafts = false
            },
            Capabilities = new AgentProfileCapabilities(),
            Validators = validators
        });
        return new GenerativeArtifactBrick(registry);
    }

    private sealed class AlwaysPassValidator : Nexo.Core.Application.Orchestration.IPostValidator<GeneratedArtifact>
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(GeneratedArtifact output, CancellationToken ct) =>
            new((true, (string?)null));
    }

    private static async Task<BrickOutput> Execute(GenerativeArtifactBrick brick, string target)
    {
        var input = new BrickInput();
        input.Set("target", target);
        return await brick.ExecuteAsync(
            input,
            ImplementationType.Agentic,
            new WiringTestContext(),
            CancellationToken.None);
    }

    private sealed class WiringTestContext : IExecutionContext
    {
        public string AgentId => "manifest-wiring-test";
        public string BehaviorId => "manifest-wiring-test";
        public bool IsAirGapped => true;
        public bool AuditMode => false;
        public string Provider => "deterministic";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
