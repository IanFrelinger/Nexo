using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Generation.Ports;
using Ashlar.Core.Application.Orchestration;
using Ashlar.Core.Application.Resilience.Ports;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Bricks.Ports;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Generation;

/// <summary>
/// Fixed first-party generative brick. Resolves an <see cref="AgentProfile"/> by
/// <c>target</c> and drives the shared repair/provenance engine. Adding a language
/// never edits this class — register a profile instead.
/// </summary>
public sealed class GenerativeArtifactBrick : GenerativeBrick
{
    private readonly IAgentProfileRegistry _registry;
    private readonly ILogger<GenerativeArtifactBrick>? _logger;
    private readonly IResilientExecutor? _resilience;
    private readonly IApprovalGate? _approvalGate;

    /// <summary>Creates the generic generative artifact brick.</summary>
    /// <param name="registry">Profile registry.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="resilience">
    /// Optional resilient executor; when present, drafter calls are retried on
    /// transient failures instead of aborting the repair loop.
    /// </param>
    /// <param name="approvalGate">
    /// Optional approval gate; when present, review-required artifacts are routed
    /// through it before deployment instead of being unconditionally blocked.
    /// </param>
    public GenerativeArtifactBrick(
        IAgentProfileRegistry registry,
        ILogger<GenerativeArtifactBrick>? logger = null,
        IResilientExecutor? resilience = null,
        IApprovalGate? approvalGate = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger;
        _resilience = resilience;
        _approvalGate = approvalGate;

        Id = "generative-artifact";
        Name = "Generative Artifact";
        Version = "1.0.0";
        Icon = "✨";
        Category = BrickCategory.Generation;
        Description = "Resolves an agent profile by target and drafts/validates/deploys an artifact.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("target", "string", "Agent profile target id"),
                new BrickInputDefinition("grounding", "string", "Context/grounding for the drafter", required: false, defaultValue: ""),
                new BrickInputDefinition("outputPath", "string", "Optional output path / deployment ref", required: false),
                new BrickInputDefinition("maxRepairAttempts", "number", "Override profile repair attempts", required: false),
                new BrickInputDefinition("preferDeterministic", "bool", "Override profile PreferDeterministic for this call", required: false),
                new BrickInputDefinition("overrides", "object", "Opaque GenerationRequest.Overrides bag for the profile", required: false),
                new BrickInputDefinition("contextHash", "string", "Canonical hash of the assembled proposal context that produced 'grounding'", required: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("artifact", "object", "GeneratedArtifact"),
                new BrickOutputDefinition(ProvenanceOutputKey, "object", "GenerativeProvenance"),
                new BrickOutputDefinition("generationStrategy", "string", "Strategy name"),
                new BrickOutputDefinition("verified", "bool", "True when validators passed"),
                new BrickOutputDefinition("deploymentResult", "object", "DeploymentApplyResult when deployment ran"),
                new BrickOutputDefinition("humanApproved", "bool", "Present when human review was required before deployment: true when the approval gate approved, false when denied/absent")
            ]
        };

        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = [ImplementationType.Agentic, ImplementationType.Deterministic];
    }

    /// <summary>
    /// The profile's validator chain, with the constraint-manifest pre-gate prepended
    /// when the profile declares a manifest (spec R3.5, second use). Prepending here —
    /// rather than in profile <c>Validators</c> arrays — keeps enforcement engine-owned:
    /// a profile cannot declare a manifest and skip its gate.
    /// </summary>
    private static IPostValidator<GeneratedArtifact>[] ResolveValidators(AgentProfile profile)
    {
        var declared = profile.Validators.OfType<IPostValidator<GeneratedArtifact>>();
        return profile.ConstraintManifest is { } manifest
            ? declared.Prepend(new BrickConstraintManifestValidator(manifest)).ToArray()
            : declared.ToArray();
    }

    /// <inheritdoc />
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var target = input.Get<string>("target", null)
            ?? throw new ArgumentException("Input 'target' is required.");
        var profile = _registry.Resolve(target)
            ?? throw new InvalidOperationException($"No agent profile registered for target '{target}'.");

        var overrides = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (input.Get<IReadOnlyDictionary<string, object>?>("overrides", null) is { } bag)
        {
            foreach (var kv in bag)
                overrides[kv.Key] = kv.Value;
        }

        var request = new GenerationRequest
        {
            TargetId = target,
            Grounding = input.Get("grounding", "") ?? "",
            // R3.5 first use: the manifest's rendered instructions travel with the
            // request so the profile's drafter puts them in the proposer prompt.
            ConstraintInstructions = profile.ConstraintManifest?.RenderInstructions() ?? "",
            ContextHash = input.Get<string>("contextHash", null),
            OutputPath = input.Get<string>("outputPath", null),
            Overrides = overrides
        };

        var maxAttempts = input.Get("maxRepairAttempts", profile.Tunables.MaxRepairAttempts);
        maxAttempts = Math.Max(1, maxAttempts);
        var preferDeterministic = input.Get<bool?>("preferDeterministic", null)
            ?? profile.Tunables.PreferDeterministic;

        GeneratedArtifact artifact;
        GenerativeProvenance provenance;

        if (preferDeterministic
            && profile.Capabilities.SupportsDeterministic
            && profile.DeterministicDrafter is { } det
            && det.TryDraft(request, out var detArtifact)
            && detArtifact is not null)
        {
            artifact = detArtifact;
            provenance = ApplyAuditPolicy(
                GenerativeProvenance.Create(
                    GenerationStrategy.Deterministic,
                    verified: true,
                    requiresHumanReview: false),
                context);
        }
        else
        {
            var validators = ResolveValidators(profile);
            var loop = new GenerationRepairLoop<GeneratedArtifact>(
                async (prior, ct) =>
                {
                    var reasons = prior.Select(p => p.Reason).ToArray();
                    // Transient provider failures must not abort the repair loop;
                    // retry them when a resilient executor is configured.
                    if (_resilience is null)
                        return await profile.Drafter.DraftAsync(request, reasons, ct).ConfigureAwait(false);
                    return await _resilience.ExecuteAsync(
                        innerCt => profile.Drafter.DraftAsync(request, reasons, innerCt),
                        RetryPolicy.Default,
                        ct).ConfigureAwait(false);
                },
                validators,
                new RepairOptions(maxAttempts));

            var result = await loop.RunAsync(cancellationToken).ConfigureAwait(false);
            artifact = result.Artifact ?? new GeneratedArtifact();
            provenance = ApplyAuditPolicy(
                GenerativeProvenance.Create(
                    GenerationStrategy.Model,
                    verified: result.Succeeded,
                    warnings: result.Feedback.Select(f => f.Reason).ToArray(),
                    // Policy, not plumbing: model drafts require review per the
                    // profile's tunable (default true), never per the caller's
                    // ImplementationType argument.
                    requiresHumanReview: !result.Succeeded || profile.Tunables.RequireHumanReviewForModelDrafts),
                context);

            if (!result.Succeeded)
            {
                _logger?.LogWarning(
                    "Generation repair exhausted for target {Target}: {Summary}",
                    target,
                    result.FailureSummary);
            }
        }

        provenance = await SynthesizeFinalProvenanceAsync(profile, artifact, provenance, context, cancellationToken)
            .ConfigureAwait(false);

        artifact = new GeneratedArtifact
        {
            Content = artifact.Content,
            Files = artifact.Files,
            Provenance = provenance
        };

        DeploymentApplyResult? deployment = null;
        bool? humanApproved = null;
        if (profile.Capabilities.SupportsDeployment
            && profile.Deployment is not null
            && provenance.Verified)
        {
            // Review-required artifacts are routed through the approval gate
            // instead of being unconditionally blocked: a human can convert
            // "requires review" into a deploy. No gate configured = deny.
            var mayDeploy = !provenance.RequiresHumanReview;
            if (!mayDeploy)
            {
                var approval = await GenerativeHumanReview.RouteAsync(
                        provenance,
                        _approvalGate,
                        $"Deploy generated artifact for target '{target}' ({provenance.Strategy} draft).",
                        AcceptanceRouting.DefaultApprovalTimeout,
                        cancellationToken)
                    .ConfigureAwait(false);
                humanApproved = approval == ApprovalResult.Approved;
                mayDeploy = humanApproved.Value;
                if (!mayDeploy)
                {
                    _logger?.LogInformation(
                        "Deployment for target {Target} withheld: human review {Approval}.",
                        target, approval);
                    deployment = new DeploymentApplyResult
                    {
                        Succeeded = false,
                        Message = $"deployment withheld: artifact requires human review (approval: {approval})"
                    };
                }
            }

            if (mayDeploy)
            {
                // AgentProfile.Acceptance is the single source of truth for the
                // post-install verdict; targets that gate on it receive it here.
                var acceptance = profile.Acceptance ?? DefaultAcceptanceEvaluator.Instance;

                // The artifact still carries RequiresHumanReview, and evaluators
                // rightly rollback on that signal. Without this the gate would be
                // theatre: the human approves, then acceptance rejects on the very
                // ground the approval just cleared. Carry the verdict forward.
                if (humanApproved == true)
                    acceptance = new HumanApprovedAcceptanceEvaluator(acceptance);

                deployment = profile.Deployment is IAcceptanceGatedDeploymentTarget gated
                    ? await gated.ApplyAsync(artifact, acceptance, cancellationToken).ConfigureAwait(false)
                    : await profile.Deployment.ApplyAsync(artifact, cancellationToken).ConfigureAwait(false);
            }
        }

        var output = new BrickOutput
        {
            Summary = provenance.Verified
                ? $"Generated artifact for target '{target}' ({provenance.Strategy})."
                : $"Generated artifact for target '{target}' with validation issues."
        };
        output.Set("artifact", artifact);
        output.Set("verified", provenance.Verified);
        if (deployment is not null)
            output.Set("deploymentResult", deployment);
        if (humanApproved is { } approved)
            output.Set("humanApproved", approved);
        EmitProvenance(output, provenance);
        return output;
    }

    /// <summary>
    /// Decorates the profile's evaluator for an artifact a human has already
    /// approved: a Rollback caused SOLELY by the pending-human-review signal
    /// becomes a Ship, because the approval gate just cleared exactly that.
    /// Every other Rollback ground — failed smoke, unverified provenance —
    /// still stands. Approval is permission to ship a *reviewed* artifact, not
    /// permission to overrule evidence.
    /// </summary>
    private sealed class HumanApprovedAcceptanceEvaluator : IAcceptanceEvaluator
    {
        private readonly IAcceptanceEvaluator _inner;

        public HumanApprovedAcceptanceEvaluator(IAcceptanceEvaluator inner) => _inner = inner;

        public AcceptanceResult Evaluate(AcceptanceContext context)
        {
            var verdict = _inner.Evaluate(context);
            if (verdict.Decision != AcceptanceDecision.Rollback
                || !AcceptanceRouting.RequiresHumanReview(verdict))
            {
                return verdict;
            }

            return AcceptanceResult.Ship(
                "approved by human review: " + verdict.Reason,
                verdict.Signals.Concat(new[] { "human-approved" }).ToArray());
        }
    }

    /// <summary>
    /// Synthesizes the final provenance for the artifact. Profile drafters may
    /// attach grounding signals (UnsupportedReferences, Confidence, Assumptions);
    /// these are preserved. Non-model paths re-run the profile's validators here
    /// (the model path was already validated inside the repair loop); the model
    /// path only merges the drafter's signals.
    /// </summary>
    private static async Task<GenerativeProvenance> SynthesizeFinalProvenanceAsync(
        AgentProfile profile,
        GeneratedArtifact artifact,
        GenerativeProvenance provenance,
        IExecutionContext context,
        CancellationToken cancellationToken)
    {
        var draftProv = artifact.Provenance;
        var postValidators = ResolveValidators(profile);
        if (provenance.Strategy != GenerationStrategy.Model && postValidators.Length > 0)
        {
            var allOk = true;
            var warnings = new List<string>();
            foreach (var v in postValidators)
            {
                var (ok, reason) = await v.ValidateAsync(artifact, cancellationToken).ConfigureAwait(false);
                if (!ok)
                {
                    allOk = false;
                    if (!string.IsNullOrWhiteSpace(reason))
                        warnings.Add(reason!);
                }
            }

            return ApplyAuditPolicy(
                GenerativeProvenance.Create(
                    provenance.Strategy,
                    verified: allOk,
                    confidence: draftProv?.Confidence,
                    warnings: MergeNotes(warnings, draftProv?.Warnings),
                    assumptions: draftProv?.Assumptions,
                    unsupportedReferences: draftProv?.UnsupportedReferences,
                    requiresHumanReview: provenance.RequiresHumanReview
                        || !allOk
                        || (draftProv?.RequiresHumanReview ?? false)),
                context);
        }

        if (draftProv is not null)
        {
            return ApplyAuditPolicy(
                GenerativeProvenance.Create(
                    provenance.Strategy,
                    verified: provenance.Verified,
                    confidence: draftProv.Confidence ?? provenance.Confidence,
                    warnings: MergeNotes(provenance.Warnings, draftProv.Warnings),
                    assumptions: draftProv.Assumptions.Count > 0 ? draftProv.Assumptions : provenance.Assumptions,
                    unsupportedReferences: draftProv.UnsupportedReferences,
                    requiresHumanReview: provenance.RequiresHumanReview || draftProv.RequiresHumanReview),
                context);
        }

        return provenance;
    }

    private static IReadOnlyList<string> MergeNotes(
        IReadOnlyList<string>? primary,
        IReadOnlyList<string>? secondary)
    {
        if (primary is null || primary.Count == 0)
            return secondary ?? Array.Empty<string>();
        if (secondary is null || secondary.Count == 0)
            return primary;
        return primary.Concat(secondary).Distinct(StringComparer.Ordinal).ToArray();
    }
}
