using FluentAssertions;
using Nexo.Agents.TestKit;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Certification.HotSwap;
using Nexo.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The proposal-iteration harness (PR-E core): intake gating before any work, per-
/// iteration attested sandbox sessions, the REAL certification chain, Tier-0 autonomous
/// swap versus held admission — every run terminating in exactly one of the four R2.3
/// states, under the R4.6/B11.2 budget ceiling.
/// </summary>
[Trait("Category", "Certification")]
[Collection("hot-swap-host")]
public sealed class AutonomousIterationHarnessTests
{
    private static readonly WitnessSpec StrongWitness = new(
        "mutation-probe-brick",
        [
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["logText"] = "2024-01-01 ERROR First failure: connection reset\n2024-01-01 ERROR Second failure: timeout"
                },
                new Dictionary<string, object>
                {
                    ["errorCount"] = 2,
                    ["firstErrorMessage"] = "First failure: connection reset"
                })
        ]);

    private static readonly TouchSet LeafTouch = new()
    {
        PathPrefixes = ["src/Nexo.Bricks.Probe/"],
        Namespaces = ["Nexo.Tests.Infrastructure.Certification.Fixtures"],
    };

    [Fact]
    public async Task IntakeGating_RefusesBeforeAnyWork()
    {
        var pause = new LoopPauseControl();
        var lineages = new InMemoryLineageAuthority();
        var harness = Harness(pause: pause, lineages: lineages);

        pause.Pause("operator hold");
        (await harness.RunIterationAsync(Context("obj-a"), Candidate()))
            .Should().Match<IterationResult>(r =>
                r.Outcome == IterationOutcome.ExplainedFailure && r.Explanation.Contains("paused"));
        pause.Resume();

        (await harness.RunIterationAsync(
                Context("obj-b") with
                {
                    Source = ObjectiveSource.Telemetry,
                    Touch = new TouchSet { PathPrefixes = ["src/Nexo.Core.Application/Autonomy/"] },
                },
                Candidate()))
            .Should().Match<IterationResult>(r =>
                r.Outcome == IterationOutcome.ExplainedFailure && r.Explanation.Contains("Tier 2"));

        lineages.Demote("obj-c", "operator decision");
        (await harness.RunIterationAsync(Context("obj-c"), Candidate()))
            .Should().Match<IterationResult>(r =>
                r.Outcome == IterationOutcome.ExplainedFailure && r.Explanation.Contains("R5.5"));

        (await harness.RunIterationAsync(
                Context("obj-d") with
                {
                    ParentEnvelope = new TouchSet { Capabilities = ["repo.fs.write"] },
                    Touch = LeafTouch with { Capabilities = ["repo.fs.write", "network.fetch"] },
                },
                Candidate()))
            .Should().Match<IterationResult>(r =>
                r.Outcome == IterationOutcome.ExplainedFailure && r.Explanation.Contains("R4.4"));
    }

    [Fact]
    public async Task EndsGood_ThroughTheHarness_AdmittedAndSwapped_WithAttestedSession()
    {
        var sandbox = new FakeSandboxedSessionRunner(FakeSandboxedSessionRunner.Success());
        var harness = Harness(sandbox: sandbox);
        var context = Context("obj-weather") with
        {
            Lineage = GenerationLineage.Child(GenerationLineage.HumanAuthored, "sig-proposer"),
            SessionSpec = new SandboxSpec(
                "proposer:latest", Array.Empty<Mount>(), NetworkAccess.None,
                new[] { "sleep", "infinity" }, new ResourceLimits(Memory: "2g")),
        };

        var result = await harness.RunIterationAsync(context, Candidate());

        result.Outcome.Should().Be(IterationOutcome.AdmittedAndSwapped, result.Explanation);
        result.Attestation.Should().NotBeNull("the session was attested before any work counted");
        result.Decision!.Record.Inputs.Should().Contain(i => i.Kind == "sandbox-spec",
            "the certificate records the environment it was minted under");
        result.Decision.Record.Inputs.Should().Contain(i => i.Kind == "generation-depth" && i.Id == "1");
        sandbox.ActiveSessions.Should().Be(0, "sessions are per-iteration and torn down with it (B9.3)");
    }

    [Fact]
    public async Task Tier1Objective_TerminatesAsCertifiedButHeld_WithEvidence()
    {
        var harness = Harness();
        var context = Context("obj-kernel") with
        {
            // A kernel PATH tiers this at 1; the candidate's own namespace stays declared
            // so its reference graph conforms — the analyzer leg judges references, the
            // classifier judges blast radius, and both must hold.
            Touch = new TouchSet
            {
                PathPrefixes = ["src/Nexo.Policies/"],
                Namespaces = ["Nexo.Tests.Infrastructure.Certification.Fixtures"],
            },
        };

        var result = await harness.RunIterationAsync(context, Candidate());

        result.Outcome.Should().Be(IterationOutcome.CertifiedButHeld, result.Explanation);
        result.Decision!.Admitted.Should().BeTrue("certification proceeds autonomously; ADMISSION waits (R3.1)");
        result.Explanation.Should().Contain("human gate");
    }

    [Fact]
    public async Task GateRejection_TerminatesAsExplainedFailure_WithProbeFindings()
    {
        var harness = Harness();
        var weakWitness = new WitnessSpec(
            "mutation-probe-brick",
            [
                new WitnessCase(
                    new Dictionary<string, object>
                    {
                        ["logText"] = "2024-01-01 ERROR First failure: connection reset\n2024-01-01 ERROR Second failure: timeout"
                    },
                    new Dictionary<string, object> { ["errorCount"] = 2 })
            ]);

        var result = await harness.RunIterationAsync(
            Context("obj-weak"), Candidate() with { Witness = weakWitness });

        result.Outcome.Should().Be(IterationOutcome.ExplainedFailure);
        result.Explanation.Should().Contain("mutation").And.Contain("probe finding(s)");
        result.Decision!.ProbeFindings.Should().NotBeEmpty("V2's probes feed the explanation");
    }

    [Fact]
    public async Task InSessionBuild_CompilesInsideTheSession_AndRecordsTheInput()
    {
        // Scripted: the toolchain probe answers a version; every later exec (uploads,
        // decode, build) repeats that success — stdout content is irrelevant to them.
        var sandbox = new FakeSandboxedSessionRunner(FakeSandboxedSessionRunner.Success("9.0.100"));
        var harness = Harness(sandbox: sandbox, buildInSession: true);

        var result = await harness.RunIterationAsync(SessionContext("obj-in-session"), Candidate());

        result.Outcome.Should().Be(IterationOutcome.AdmittedAndSwapped, result.Explanation);
        result.Decision!.Record.Inputs.Should().Contain(i => i.Kind == "session-build",
            "a passed in-session build is certificate evidence, bound to the source hash");

        var session = sandbox.Sessions.Single();
        session.ExecCommands.First().Should().Equal(new[] { "dotnet", "--version" },
            "the leg probes the toolchain before uploading anything");
        session.ExecCommands.Should().Contain(c => c.Count >= 2 && c[0] == "dotnet" && c[1] == "build",
            "the candidate compiles via the session, not the harness process");
        sandbox.ActiveSessions.Should().Be(0, "the build leg must not leak the session");
    }

    [Fact]
    public async Task InSessionBuild_ToolchainOrStepFailure_TerminatesAsExplainedFailure()
    {
        // First exec (the probe) fails: no dotnet in the image.
        var noToolchain = new FakeSandboxedSessionRunner(
            FakeSandboxedSessionRunner.Failure("sh: dotnet: not found", 127));
        var harness = Harness(sandbox: noToolchain, buildInSession: true);

        var result = await harness.RunIterationAsync(SessionContext("obj-no-dotnet"), Candidate());

        result.Outcome.Should().Be(IterationOutcome.ExplainedFailure);
        result.Explanation.Should().Contain("session-build").And.Contain("dotnet");
        result.Decision.Should().BeNull("the candidate never reached the gate");
        noToolchain.ActiveSessions.Should().Be(0, "failure paths tear the session down too");

        // Probe passes, the very next step (work-directory reset) fails.
        var midFailure = new FakeSandboxedSessionRunner(
            FakeSandboxedSessionRunner.Success("9.0.100"),
            FakeSandboxedSessionRunner.Failure("mkdir: cannot create directory", 1));
        harness = Harness(sandbox: midFailure, buildInSession: true);

        result = await harness.RunIterationAsync(SessionContext("obj-mid-fail"), Candidate());

        result.Outcome.Should().Be(IterationOutcome.ExplainedFailure);
        result.Explanation.Should().Contain("session-build");
        midFailure.ActiveSessions.Should().Be(0);
    }

    [Fact]
    public async Task InSessionBuild_WithoutASession_RefusesFailClosed()
    {
        // The flag demands compilation containment; an iteration with no session must
        // refuse, never quietly fall back to building on the host.
        var noSpec = Harness(sandbox: new FakeSandboxedSessionRunner(
            FakeSandboxedSessionRunner.Success()), buildInSession: true);
        (await noSpec.RunIterationAsync(Context("obj-no-spec"), Candidate()))
            .Should().Match<IterationResult>(r =>
                r.Outcome == IterationOutcome.ExplainedFailure
                && r.Explanation.Contains("fail-closed")
                && r.Decision == null);

        var noRunner = Harness(sandbox: null, buildInSession: true);
        (await noRunner.RunIterationAsync(SessionContext("obj-no-runner"), Candidate()))
            .Should().Match<IterationResult>(r =>
                r.Outcome == IterationOutcome.ExplainedFailure && r.Explanation.Contains("fail-closed"));
    }

    [Fact]
    public async Task BudgetCeiling_TerminatesAsBudgetExhausted()
    {
        var harness = Harness(budget: new ClusterBudget(TimeSpan.FromMilliseconds(1)));

        var result = await harness.RunIterationAsync(Context("obj-slow"), Candidate());

        result.Outcome.Should().Be(IterationOutcome.BudgetExhausted);
        result.Explanation.Should().Contain("wall-clock ceiling");
    }

    [Fact]
    public void ThroughputGuard_TightensTheCeiling_OnceTheMedianForms()
    {
        var budget = new ClusterBudget(TimeSpan.FromMinutes(10), throughputGuardFactor: 4);
        budget.EffectiveCeiling().Should().Be(TimeSpan.FromMinutes(10), "no median yet");

        budget.RecordCompletion(TimeSpan.FromSeconds(10));
        budget.RecordCompletion(TimeSpan.FromSeconds(12));
        budget.RecordCompletion(TimeSpan.FromSeconds(14));

        budget.EffectiveCeiling().Should().Be(TimeSpan.FromSeconds(48),
            "median 12s x factor 4 — one degenerate session cannot starve the cadence (R4.6)");
    }

    // --- helpers -------------------------------------------------------------------------

    private static AutonomousIterationHarness Harness(
        LoopPauseControl? pause = null,
        ILineageAuthority? lineages = null,
        ISandboxedSessionRunner? sandbox = null,
        ClusterBudget? budget = null,
        bool buildInSession = false) =>
        new(
            new CertificationGate(new CertificationRecordSigner()),
            new CertifiedBrickHotSwapHost(
                hmacKey: null, drainTimeout: TimeSpan.FromSeconds(10),
                revocations: new InMemoryCertificateRevocationList(),
                lineageAuthority: lineages),
            pause, lineages, sandbox, budget,
            buildCandidateInSession: buildInSession);

    private static ProposalIterationContext Context(string objectiveId) => new()
    {
        ObjectiveId = objectiveId,
        Source = ObjectiveSource.Triage,
        Touch = LeafTouch,
    };

    private static ProposalIterationContext SessionContext(string objectiveId) =>
        Context(objectiveId) with
        {
            SessionSpec = new SandboxSpec(
                "sdk:pinned", Array.Empty<Mount>(), NetworkAccess.None,
                new[] { "sleep", "infinity" }, new ResourceLimits(Memory: "2g")),
        };

    private static ProposalCandidate Candidate() => new()
    {
        Brick = new MutationProbeBrick(),
        SourceCode = MutationProbeBrickSource.Code,
        Witness = StrongWitness,
        ProjectPath = CleanProjectFile(),
        CompilationReferences =
        [
            typeof(DomainBrick).Assembly.Location,
            typeof(BrickInput).Assembly.Location,
            typeof(MutationProbeBrick).Assembly.Location
        ],
        BrickTypeName = typeof(MutationProbeBrick).FullName,
    };

    private static string CleanProjectFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-harness-{Guid.NewGuid():N}.csproj");
        File.WriteAllText(path, """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Nexo.Brick.Contracts" Version="0.1.0" />
  </ItemGroup>
</Project>
""");
        return path;
    }
}
