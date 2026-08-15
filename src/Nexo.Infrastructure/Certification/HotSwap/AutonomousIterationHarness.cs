using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Infrastructure.Certification.HotSwap;

/// <summary>The four terminal states of one loop iteration (autonomy spec R2.3).</summary>
public enum IterationOutcome
{
    /// <summary>Certified and hot-swapped without human involvement (Tier 0).</summary>
    AdmittedAndSwapped,

    /// <summary>Certified; admission is holding for the human gate (or the swap host held it).</summary>
    CertifiedButHeld,

    /// <summary>The iteration failed with its evidence attached — including intake refusals.</summary>
    ExplainedFailure,

    /// <summary>The budget ceiling expired; partial artifacts retained.</summary>
    BudgetExhausted,
}

/// <summary>One iteration's terminal state with its evidence.</summary>
public sealed record IterationResult(
    IterationOutcome Outcome,
    string Explanation,
    TierClassification? Tier = null,
    CertificationDecision? Decision = null,
    SessionAttestation? Attestation = null);

/// <summary>The context an iteration runs under: the objective's declarations, projected to core types.</summary>
public sealed record ProposalIterationContext
{
    /// <summary>Objective id; doubles as the lineage key for demotion tracking (R5.5).</summary>
    public required string ObjectiveId { get; init; }

    /// <summary>Objective source in R1.1 trust order.</summary>
    public required ObjectiveSource Source { get; init; }

    /// <summary>Declared touch-set; null classifies fail-closed (R1.4).</summary>
    public TouchSet? Touch { get; init; }

    /// <summary>Parent session's envelope for depth&gt;0 iterations; child must be within it (R4.4).</summary>
    public TouchSet? ParentEnvelope { get; init; }

    /// <summary>Recursion pedigree (R4.1); null = human-authored context.</summary>
    public GenerationLineage? Lineage { get; init; }

    /// <summary>Constraint manifest, same instance the prompt was rendered from (A2.3).</summary>
    public BrickConstraintManifest? Manifest { get; init; }

    /// <summary>Sandbox spec for the iteration's session; null = no session (attestation skipped).</summary>
    public SandboxSpec? SessionSpec { get; init; }
}

/// <summary>What the proposer produced. Producing it (the model cluster) is outside this harness.</summary>
public sealed record ProposalCandidate
{
    /// <summary>Brick instance for witness execution.</summary>
    public required DomainBrick Brick { get; init; }

    /// <summary>Candidate source (hash-bound into the certificate).</summary>
    public required string SourceCode { get; init; }

    /// <summary>Witness cases.</summary>
    public required WitnessSpec Witness { get; init; }

    /// <summary>Project path for dependency checks.</summary>
    public required string ProjectPath { get; init; }

    /// <summary>Compilation references.</summary>
    public IReadOnlyList<string> CompilationReferences { get; init; } = Array.Empty<string>();

    /// <summary>Optional brick type name override.</summary>
    public string? BrickTypeName { get; init; }
}

/// <summary>
/// One loop iteration end to end (the PR-E core): intake gating (pause, tier-source,
/// lineage demotion, envelope narrowing) → optional sandbox session with attestation →
/// certification with the objective's declarations → Tier-0 autonomous swap or held
/// admission — terminating in exactly one of the four R2.3 states. Sessions are opened
/// per iteration and torn down with it (B9.3 restart-on-patch holds by construction: no
/// session outlives the image decision it started under). The budget ceiling (R4.6 /
/// B11.2) bounds the whole iteration's wall clock, tightened by the throughput guard.
///
/// <para><b>What the session does and does not do today.</b> When a
/// <see cref="ProposalIterationContext.SessionSpec"/> is supplied, the session is started,
/// attested (image digest, engine version, effective resource caps — refused if weaker
/// than requested), recorded onto the certificate as environment inputs, and torn down
/// with the iteration. With the in-session build leg enabled
/// (<c>buildCandidateInSession</c>), the harness additionally compiles the candidate
/// INSIDE the session via <see cref="ISandboxedSession.ExecAsync"/> — the exact wrapped
/// bytes every certification-path compile sees (<see cref="SessionCandidateBuild"/>) —
/// refusing fail-closed when the leg is demanded but no session exists, and recording a
/// <c>session-build</c> input on the certificate when it passes. The witness run and
/// mutation run still happen <b>in the harness process</b>: <c>sandbox-spec</c>/
/// <c>attestation</c> inputs remain provisioning evidence, <c>session-build</c> claims
/// compilation containment only, and nothing yet claims contained execution. The write
/// surface IS confined separately, by <c>ProposerConfinement</c>'s tool allowlist.</para>
/// </summary>
public sealed class AutonomousIterationHarness
{
    private readonly ICertificationGate _gate;
    private readonly CertifiedBrickHotSwapHost _host;
    private readonly LoopPauseControl? _pause;
    private readonly ILineageAuthority? _lineages;
    private readonly ISandboxedSessionRunner? _sandbox;
    private readonly ClusterBudget _budget;
    private readonly ILogger<AutonomousIterationHarness>? _logger;
    private readonly bool _buildCandidateInSession;
    private readonly bool _executeCandidateInSession;

    /// <summary>
    /// Creates the harness over the real gate and swap host. With
    /// <c>buildCandidateInSession</c> true, every iteration MUST compile its candidate
    /// inside the attested session (<see cref="SessionCandidateBuild"/>); an iteration
    /// without a session then refuses fail-closed rather than silently building on the host.
    /// With <c>executeCandidateInSession</c> additionally true, the gate's witness,
    /// determinism, and mutation legs EXECUTE candidate and mutant code inside that same
    /// session (<see cref="SessionExecutionBackend"/> over the session-built assembly) —
    /// untrusted candidate code then never runs in this process.
    /// </summary>
    public AutonomousIterationHarness(
        ICertificationGate gate,
        CertifiedBrickHotSwapHost host,
        LoopPauseControl? pause = null,
        ILineageAuthority? lineages = null,
        ISandboxedSessionRunner? sandbox = null,
        ClusterBudget? budget = null,
        ILogger<AutonomousIterationHarness>? logger = null,
        bool buildCandidateInSession = false,
        bool executeCandidateInSession = false)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _pause = pause;
        _lineages = lineages;
        _sandbox = sandbox;
        _budget = budget ?? new ClusterBudget();
        _logger = logger;
        _buildCandidateInSession = buildCandidateInSession;
        _executeCandidateInSession = executeCandidateInSession;
    }

    /// <summary>Runs one iteration to a terminal state. Never throws for loop-shaped failures.</summary>
    public async Task<IterationResult> RunIterationAsync(
        ProposalIterationContext context,
        ProposalCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        // --- intake gating: refusals are explained failures that never start work ------
        if (_pause?.IsPaused == true)
        {
            return new IterationResult(IterationOutcome.ExplainedFailure,
                $"intake refused: the loop is paused ({_pause.PausedReason}); objective intake halts immediately (R6.2)");
        }

        var tier = ObjectiveTierClassifier.Classify(context.Touch ?? new TouchSet());
        if (!tier.MaySessionOpen(context.Source))
        {
            return new IterationResult(IterationOutcome.ExplainedFailure,
                "intake refused: Tier 2 objectives must originate from a human; a machine source may not open a session (R3.1)",
                tier);
        }

        if (_lineages?.IsDemoted(context.ObjectiveId) == true)
        {
            return new IterationResult(IterationOutcome.ExplainedFailure,
                $"intake refused: lineage '{context.ObjectiveId}' lost Tier-0 autonomy on rollback evidence (R5.5)",
                tier);
        }

        if (context.ParentEnvelope is { } parent && context.Touch is { } childTouch)
        {
            var widenings = TouchSetNarrowing.FindWidenings(childTouch, parent);
            if (widenings.Count > 0)
            {
                return new IterationResult(IterationOutcome.ExplainedFailure,
                    "intake refused: the child session widens beyond its parent's envelope (R4.4): "
                    + string.Join(" | ", widenings),
                    tier);
            }
        }

        // --- the budgeted run ----------------------------------------------------------
        var ceiling = _budget.EffectiveCeiling();
        using var budgetCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budgetCts.CancelAfter(ceiling);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await RunBudgetedAsync(context, candidate, tier, budgetCts.Token).ConfigureAwait(false);
            _budget.RecordCompletion(stopwatch.Elapsed);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The ceiling, not the caller: terminal state four, artifacts retained
            // (whatever certification recorded before the ceiling is already persisted
            // by the caller's stores; nothing here is discarded).
            return new IterationResult(IterationOutcome.BudgetExhausted,
                $"budget exhausted: the iteration exceeded its wall-clock ceiling of {ceiling.TotalSeconds:F0}s "
                + "(R4.6/B11.2; the throughput guard tightens this as the cluster's median forms)",
                tier);
        }
    }

    private async Task<IterationResult> RunBudgetedAsync(
        ProposalIterationContext context,
        ProposalCandidate candidate,
        TierClassification tier,
        CancellationToken cancellationToken)
    {
        SessionAttestation? attestation = null;
        var environmentInputs = Array.Empty<CertificationInput>() as IReadOnlyList<CertificationInput>;

        ISandboxedSession? session = null;
        try
        {
            // Per-iteration session (B9.3 by construction), attested before any work counts.
            if (_sandbox is not null && context.SessionSpec is { } spec)
            {
                session = await _sandbox.StartAsync(spec, cancellationToken).ConfigureAwait(false);
                attestation = await session.AttestAsync(cancellationToken).ConfigureAwait(false);
                attestation.ThrowIfBelowRequested();
                environmentInputs = SessionEnvironmentInputs.From(spec, attestation);
            }

            if (_buildCandidateInSession)
            {
                if (session is null)
                {
                    // Fail-closed: the configuration demanded compilation containment, and
                    // building on the host instead would silently drop it (R1.4 shape).
                    return new IterationResult(IterationOutcome.ExplainedFailure,
                        "session-build refused: the harness requires the candidate to build inside an "
                        + "attested session, but this iteration has none (no SessionSpec supplied, or no "
                        + "session runner composed); building on the host instead would silently drop the "
                        + "demanded containment (fail-closed)",
                        tier);
                }

                var build = await SessionCandidateBuild.RunAsync(session, candidate, cancellationToken)
                    .ConfigureAwait(false);
                if (!build.Passed)
                {
                    return new IterationResult(IterationOutcome.ExplainedFailure,
                        $"session-build failed (exit {build.ExitCode}, toolchain '{build.ToolchainVersion}'): "
                        + build.OutputTail,
                        tier, null, attestation);
                }

                _logger?.LogInformation(
                    "Candidate for {ObjectiveId} compiled in session {SessionId} (toolchain {Toolchain})",
                    context.ObjectiveId, session.SessionId, build.ToolchainVersion);
                environmentInputs = environmentInputs
                    .Append(SessionCandidateBuild.ToInput(build, session.SessionId, candidate.SourceCode))
                    .ToArray();
            }

            // The execution leg rides the build leg: it loads the session-built assembly,
            // so demanding it without the build is a configuration error the validator
            // refuses at boot. Reaching here without a session is impossible for the same
            // reason (the build leg above already refused).
            SessionExecutionBackend? executionBackend = null;
            if (_executeCandidateInSession && session is not null)
                executionBackend = new SessionExecutionBackend(session);

            var decision = await _gate.CertifyAsync(new CertificationRequest
            {
                Brick = candidate.Brick,
                Witness = candidate.Witness,
                SourceCode = candidate.SourceCode,
                ProjectPath = candidate.ProjectPath,
                CompilationReferences = candidate.CompilationReferences,
                BrickTypeName = candidate.BrickTypeName,
                ConstraintManifest = context.Manifest,
                TouchSet = context.Touch,
                Lineage = context.Lineage,
                AdditionalInputs = environmentInputs,
                ExecutionBackend = executionBackend,
            }, cancellationToken).ConfigureAwait(false);

            if (!decision.Admitted)
            {
                return new IterationResult(IterationOutcome.ExplainedFailure,
                    $"certification rejected at '{decision.FailureCheck}' with {decision.ProbeFindings.Count} probe finding(s): "
                    + decision.Record.Reason,
                    tier, decision, attestation);
            }

            if (tier.Tier != ObjectiveTier.Tier0Autonomous)
            {
                return new IterationResult(IterationOutcome.CertifiedButHeld,
                    $"certified; admission holds for the human gate (tier {tier.Tier}, R3.1) with full evidence on the record",
                    tier, decision, attestation);
            }

            var swap = await _host.SwapAsync(new[]
            {
                new CertifiedBrickLoadRequest
                {
                    BrickId = decision.Record.BrickId,
                    SourceCode = candidate.SourceCode,
                    Record = CertificationRecordMapper.ToData(decision.Record),
                    BrickTypeName = candidate.BrickTypeName,
                    AdditionalCompilationReferences = candidate.CompilationReferences,
                    Autonomous = new AutonomousAdmission
                    {
                        Tier = tier.Tier,
                        Lineage = context.Lineage,
                        LineageKey = context.ObjectiveId,
                    },
                }
            }, cancellationToken).ConfigureAwait(false);

            if (swap.Swapped)
            {
                return new IterationResult(IterationOutcome.AdmittedAndSwapped,
                    $"certified and swapped as generation {swap.GenerationId}; the watch window is now the last gate",
                    tier, decision, attestation);
            }

            // A refused autonomous swap (cadence floor, in-flight window, pause landing
            // mid-run, lineage demotion racing in) is a HELD certificate, not a failure:
            // the artifact and its evidence persist for the next admission opportunity.
            return new IterationResult(IterationOutcome.CertifiedButHeld,
                "certified; the swap host held admission: "
                + string.Join(" | ", swap.Refusals.Select(r => $"{r.FailureCode}: {r.Reason}")),
                tier, decision, attestation);
        }
        finally
        {
            if (session is not null)
                await session.DisposeAsync().ConfigureAwait(false);
        }
    }
}
