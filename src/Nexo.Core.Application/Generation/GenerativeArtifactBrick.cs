using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Generation;

/// <summary>
/// Fixed first-party generative brick. Resolves an <see cref="AgentProfile"/> by
/// <c>target</c> and drives the shared repair/provenance engine. Adding a language
/// never edits this class — register a profile instead.
/// </summary>
public sealed class GenerativeArtifactBrick : GenerativeBrick
{
    private readonly IAgentProfileRegistry _registry;
    private readonly ILogger<GenerativeArtifactBrick>? _logger;

    /// <summary>Creates the generic generative artifact brick.</summary>
    public GenerativeArtifactBrick(
        IAgentProfileRegistry registry,
        ILogger<GenerativeArtifactBrick>? logger = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger;

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
                new BrickInputDefinition("overrides", "object", "Opaque GenerationRequest.Overrides bag for the profile", required: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("artifact", "object", "GeneratedArtifact"),
                new BrickOutputDefinition(ProvenanceOutputKey, "object", "GenerativeProvenance"),
                new BrickOutputDefinition("generationStrategy", "string", "Strategy name"),
                new BrickOutputDefinition("verified", "bool", "True when validators passed"),
                new BrickOutputDefinition("deploymentResult", "object", "DeploymentApplyResult when deployment ran")
            ]
        };

        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = [ImplementationType.Agentic, ImplementationType.Deterministic];
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
                    GenerationStrategy.Templated,
                    verified: true,
                    requiresHumanReview: false),
                context);
        }
        else
        {
            var validators = profile.Validators.OfType<IPostValidator<GeneratedArtifact>>().ToArray();
            var loop = new GenerationRepairLoop<GeneratedArtifact>(
                async (prior, ct) =>
                {
                    var reasons = prior.Select(p => p.Reason).ToArray();
                    return await profile.Drafter.DraftAsync(request, reasons, ct).ConfigureAwait(false);
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
                    requiresHumanReview: !result.Succeeded || implementation == ImplementationType.Agentic),
                context);

            if (!result.Succeeded)
            {
                _logger?.LogWarning(
                    "Generation repair exhausted for target {Target}: {Summary}",
                    target,
                    result.FailureSummary);
            }
        }

        // Profile drafters may attach grounding signals (UnsupportedReferences,
        // Confidence, Assumptions). Preserve them when the brick synthesizes the
        // final provenance. Re-run validators on non-model paths even when the
        // drafter already set artifact.Provenance (model path already validated
        // inside GenerationRepairLoop).
        var draftProv = artifact.Provenance;
        var postValidators = profile.Validators.OfType<IPostValidator<GeneratedArtifact>>().ToArray();
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

            provenance = ApplyAuditPolicy(
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
        else if (draftProv is not null)
        {
            provenance = ApplyAuditPolicy(
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

        artifact = new GeneratedArtifact
        {
            Content = artifact.Content,
            Files = artifact.Files,
            Provenance = provenance
        };

        DeploymentApplyResult? deployment = null;
        if (profile.Capabilities.SupportsDeployment
            && profile.Deployment is not null
            && provenance.Verified
            && !provenance.RequiresHumanReview)
        {
            // AgentProfile.Acceptance is the single source of truth for the
            // post-install verdict; targets that gate on it receive it here.
            var acceptance = profile.Acceptance ?? DefaultAcceptanceEvaluator.Instance;
            deployment = profile.Deployment is IAcceptanceGatedDeploymentTarget gated
                ? await gated.ApplyAsync(artifact, acceptance, cancellationToken).ConfigureAwait(false)
                : await profile.Deployment.ApplyAsync(artifact, cancellationToken).ConfigureAwait(false);
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
        EmitProvenance(output, provenance);
        return output;
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
