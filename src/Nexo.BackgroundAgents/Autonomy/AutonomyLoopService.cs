using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Objectives;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Certification.HotSwap;

namespace Nexo.BackgroundAgents.Autonomy;

/// <summary>Where the loop runs and what it may do — the operator's dial.</summary>
public sealed class AutonomyLoopSettings
{
    /// <summary>Seconds between sweeps of the objective store. 0 disables the loop.</summary>
    public int IntervalSeconds { get; set; }

    /// <summary>
    /// When true (the default), certify fully but admit nothing without a human. This is
    /// deliberately the default: a loop that starts swapping the moment it is wired up
    /// gives its operator no chance to read the evidence first.
    /// </summary>
    public bool HoldAdmission { get; set; } = true;

    /// <summary>Container image proposal sessions run in.</summary>
    public string? SessionImage { get; set; }

    /// <summary>Objectives attempted per sweep. Keeps one sweep bounded.</summary>
    public int MaxObjectivesPerSweep { get; set; } = 1;

    /// <summary>Reference assemblies handed to the candidate compile.</summary>
    public IReadOnlyList<string> CompilationReferences { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The repair channel's dial: how much of a rejection the proposer may see, and how many
    /// repair rounds an objective gets before it is held for a human. Model-independent by
    /// design — a large hosted model and a small local one need different settings on the
    /// same loop. Default: locations and the proposer's own output, never the witness;
    /// two attempts.
    /// </summary>
    public RepairFeedbackPolicy Repair { get; set; } = RepairFeedbackPolicy.Default();
}

/// <summary>
/// The standing loop: sweeps the objective store and, for each eligible objective, drives
/// one iteration through <see cref="AutonomousIterationHarness"/>.
///
/// <para>This is the piece that turns the loop from a mechanism into a running system.
/// Every part it uses already existed and was proven in flight — intake gating, tier
/// classification, attested sessions, in-session build and execution, the swap host,
/// cadence, budgets, pause. What was missing was anything that CALLED them on a schedule
/// against a real backlog.</para>
///
/// <para><b>Fail-closed at intake, by design.</b> An objective runs only if a
/// human-authored witness sits beside it and a candidate exists to certify. No witness
/// means no run — never a run without acceptance criteria, because a certificate minted
/// without them would be a claim about nothing. Proposal and iteration failures alike
/// record an attempt and move on: one bad objective must not wedge the sweep.</para>
/// </summary>
public sealed class AutonomyLoopService : BackgroundService
{
    private readonly IObjectiveStore _objectives;
    private readonly AutonomousIterationHarness _harness;
    private readonly IProposalSource? _proposals;
    private readonly AutonomyLoopSettings _settings;
    private readonly ILogger<AutonomyLoopService> _logger;

    /// <summary>Creates the loop service.</summary>
    public AutonomyLoopService(
        IObjectiveStore objectives,
        AutonomousIterationHarness harness,
        AutonomyLoopSettings settings,
        ILogger<AutonomyLoopService> logger,
        IProposalSource? proposals = null)
    {
        _objectives = objectives ?? throw new ArgumentNullException(nameof(objectives));
        _harness = harness ?? throw new ArgumentNullException(nameof(harness));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _proposals = proposals;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_settings.IntervalSeconds <= 0)
        {
            _logger.LogInformation(
                "Autonomy loop disabled (IntervalSeconds={Interval})", _settings.IntervalSeconds);
            return;
        }

        _logger.LogInformation(
            "Autonomy loop starting: every {Interval}s, holdAdmission={Hold}",
            _settings.IntervalSeconds, _settings.HoldAdmission);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.IntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await SweepAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // A sweep failure is never fatal: the loop's whole value is that it
                    // keeps running and keeps reporting.
                    _logger.LogWarning(ex, "Autonomy sweep failed; the cadence continues");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Host shutdown.
        }
    }

    /// <summary>Runs one sweep. Exposed so tests drive it directly instead of on a timer.</summary>
    public async Task<int> SweepAsync(CancellationToken cancellationToken = default)
    {
        var pending = _objectives.List(ObjectiveStatus.Pending);
        var attempted = 0;

        foreach (var objective in pending.OrderBy(o => o.Priority).ThenBy(o => o.CreatedAt))
        {
            if (attempted >= _settings.MaxObjectivesPerSweep || cancellationToken.IsCancellationRequested)
                break;

            var path = ObjectivePath(objective);
            if (path is null)
                continue;

            var witness = ObjectiveArtifacts.LoadWitness(path);
            if (witness is null)
            {
                // Not worth shouting about each sweep: an objective simply is not eligible
                // until a human has written its acceptance criteria.
                _logger.LogDebug(
                    "Objective {Id} skipped: no witness beside it ({Path})", objective.Id, path);
                continue;
            }

            var proposal = await ProposeAsync(objective, path, cancellationToken).ConfigureAwait(false);
            if (proposal is null)
            {
                _logger.LogDebug("Objective {Id} skipped: no proposal available", objective.Id);
                continue;
            }

            attempted++;
            await RunOneAsync(objective, witness, proposal, cancellationToken).ConfigureAwait(false);
        }

        return attempted;
    }

    private string? ObjectivePath(ObjectiveDocument objective)
    {
        if (_objectives is not ObjectiveStore store)
            return null;

        foreach (var status in Enum.GetValues<ObjectiveStatus>())
        {
            var candidate = Path.Combine(store.Location, status.ToString().ToLowerInvariant(), objective.Id + ".md");
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private async Task<ProposedSource?> ProposeAsync(
        ObjectiveDocument objective, string path, CancellationToken cancellationToken)
    {
        if (_proposals is not null)
        {
            var request = new ProposalRequest(objective.Id, objective.Title, objective.Body, objective.Touch);
            return await _proposals.ProposeAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // No live proposer composed: fall back to a recorded proposal beside the objective
        // (the record/replay discipline the dogfood arcs established).
        return ObjectiveArtifacts.LoadRecordedProposal(path);
    }

    /// <summary>
    /// Runs the objective through the harness, and — when a live proposer is composed and
    /// the verdict is a rejection with something to say (a certification decision, or a
    /// candidate that did not compile) — hands the proposer a policy-projected view of that
    /// rejection and tries again, up to <see cref="RepairFeedbackPolicy.MaxAttemptsPerObjective"/>
    /// repairs. Every attempt is its own full iteration (session, attestation, chain), so the
    /// evidence for each round stands on its own; nothing here shortcuts the gate.
    /// </summary>
    private async Task RunOneAsync(
        ObjectiveDocument objective,
        WitnessSpec witness,
        ProposedSource proposal,
        CancellationToken cancellationToken)
    {
        var policy = _settings.Repair;
        var current = proposal;

        for (var attempt = 0; ; attempt++)
        {
            var result = await RunIterationAsync(objective, witness, current, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Autonomy iteration for {Id} (attempt {Attempt}): {Outcome} — {Explanation}",
                objective.Id, attempt + 1, result.Outcome, result.Explanation);

            // Repair a certification rejection with a decision to project, or a candidate
            // that did not compile (its diagnostics ride on the result); intake refusals,
            // budget exhaustion, and held/admitted outcomes are terminal here. Dogfood
            // campaign 1 showed why the build case matters: a small model's dominant failure
            // is a one-line compile slip, and until this it was terminal after one attempt.
            if (result.Outcome != IterationOutcome.ExplainedFailure)
                return;
            var repairable = result.Decision is not null || result.BuildDiagnostics is not null;
            if (!repairable)
                return;
            if (_proposals is null || attempt >= policy.MaxAttemptsPerObjective)
            {
                if (_proposals is not null)
                {
                    _logger.LogInformation(
                        "Objective {Id}: repair budget of {Max} exhausted; holding for a human",
                        objective.Id, policy.MaxAttemptsPerObjective);
                }
                return;
            }

            // The proposer sees the projection, never the raw rejection.
            var feedback = result.Decision is not null
                ? RepairFeedback.Render(result.Decision, policy)
                : RepairFeedback.RenderBuildFailure(result.BuildDiagnostics!, policy);
            var request = new ProposalRequest(objective.Id, objective.Title, objective.Body, objective.Touch)
            {
                Repair = new RepairContext(
                    current.SourceCode,
                    feedback,
                    attempt + 1,
                    result.Decision is not null ? RepairKind.Certification : RepairKind.Build),
            };
            var repaired = await _proposals.ProposeAsync(request, cancellationToken).ConfigureAwait(false);
            if (repaired is null)
            {
                _logger.LogInformation("Objective {Id}: proposer declined to repair", objective.Id);
                return;
            }

            current = repaired;
        }
    }

    private async Task<IterationResult> RunIterationAsync(
        ObjectiveDocument objective,
        WitnessSpec witness,
        ProposedSource proposal,
        CancellationToken cancellationToken)
    {
        var context = new ProposalIterationContext
        {
            ObjectiveId = objective.Id,
            Source = objective.Source,
            Touch = objective.Touch,
            // The proposer signature is the provenance channel (R4.1): it rides the lineage
            // and is hash-bound into the certificate's generation-depth input.
            Lineage = GenerationLineage.Child(GenerationLineage.HumanAuthored, proposal.ProposerSignature),
            SessionSpec = new SandboxSpec(
                Image: _settings.SessionImage,
                // No mounts: sessions may be sibling containers on another daemon, and the
                // candidate travels in over ExecAsync rather than through the filesystem.
                Mounts: Array.Empty<Mount>(),
                Network: NetworkAccess.None,
                Command: new[] { "sleep", "600" },
                Limits: new ResourceLimits(Memory: "512m", Pids: 128, Cpus: "1")),
        };

        var candidate = new ProposalCandidate
        {
            Brick = new ProposedBrickHandle(witness.BrickId),
            SourceCode = proposal.SourceCode,
            Witness = witness,
            ProjectPath = CleanProjectFile(),
            CompilationReferences = _settings.CompilationReferences,
            BrickTypeName = proposal.TypeName,
        };

        return await _harness.RunIterationAsync(context, candidate, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string CleanProjectFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-objective-{Guid.NewGuid():N}.csproj");
        File.WriteAllText(
            path,
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n"
            + "    <PackageReference Include=\"Nexo.Brick.Contracts\" Version=\"0.1.0\" />\n"
            + "  </ItemGroup>\n</Project>\n");
        return path;
    }
}
