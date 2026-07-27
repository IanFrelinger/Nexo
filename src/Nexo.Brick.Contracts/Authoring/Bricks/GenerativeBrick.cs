using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Base for bricks that emit <see cref="GenerativeProvenance"/>.
/// Honors <see cref="IExecutionContext.AuditMode"/> by forcing human review
/// for model-authored artifacts. Approval routing is done via
/// <see cref="RouteHumanReviewIfRequiredAsync"/> with an injected gate callback
/// so this contracts package stays free of infrastructure dependencies.
/// </summary>
public abstract class GenerativeBrick : Brick
{
    /// <summary>BrickOutput key for <see cref="GenerativeProvenance"/>.</summary>
    public const string ProvenanceOutputKey = "generativeProvenance";

    /// <summary>
    /// Applies audit-mode policy: model strategy under audit always requires human review.
    /// </summary>
    protected static GenerativeProvenance ApplyAuditPolicy(
        GenerativeProvenance provenance,
        IExecutionContext context)
    {
        if (provenance is null) throw new ArgumentNullException(nameof(provenance));
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (context.AuditMode && provenance.Strategy == GenerationStrategy.Model && !provenance.RequiresHumanReview)
        {
            return new GenerativeProvenance
            {
                Strategy = provenance.Strategy,
                Verified = provenance.Verified,
                Confidence = provenance.Confidence,
                Warnings = provenance.Warnings,
                Assumptions = provenance.Assumptions,
                UnsupportedReferences = provenance.UnsupportedReferences,
                RequiresHumanReview = true
            };
        }

        return provenance;
    }

    /// <summary>Writes provenance onto the brick output bag.</summary>
    protected static void EmitProvenance(BrickOutput output, GenerativeProvenance provenance)
    {
        if (output is null) throw new ArgumentNullException(nameof(output));
        if (provenance is null) throw new ArgumentNullException(nameof(provenance));
        output.Set(ProvenanceOutputKey, provenance);
        output.Set("generationStrategy", provenance.Strategy.ToString());
    }

    /// <summary>
    /// When <paramref name="provenance"/> requires human review, invokes
    /// <paramref name="requestApproval"/>. Returns true when approved (or when
    /// review is not required). A null gate denies review-required work.
    /// </summary>
    /// <summary>
    /// Public entry for approval routing (used by install/deploy hosts).
    /// </summary>
    public static async Task<bool> RouteHumanReviewIfRequiredAsync(
        GenerativeProvenance provenance,
        Func<string, TimeSpan, CancellationToken, Task<bool>>? requestApproval,
        string actionDescription,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (provenance is null) throw new ArgumentNullException(nameof(provenance));
        if (!provenance.RequiresHumanReview)
            return true;

        if (requestApproval is null)
            return false;

        return await requestApproval(actionDescription, timeout, cancellationToken).ConfigureAwait(false);
    }
}
