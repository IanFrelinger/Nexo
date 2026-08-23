using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Domain.Bricks.Ports;

namespace Ashlar.Core.Application.Generation;

/// <summary>
/// Bridges the pure <see cref="IAcceptanceEvaluator"/> verdict to the existing
/// <see cref="IApprovalGate"/> port. Evaluators stay side-effect free — they emit
/// <see cref="DefaultAcceptanceEvaluator.HumanReviewSignal"/> and this decides
/// whether a human can convert that Rollback into a Ship.
/// </summary>
public static class AcceptanceRouting
{
    /// <summary>Default wait for a human decision.</summary>
    public static readonly TimeSpan DefaultApprovalTimeout = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Evaluates acceptance and, when the verdict is a Rollback caused solely by
    /// pending human review, routes it through <paramref name="gate"/>. A null gate
    /// leaves the Rollback standing (deny by default).
    /// </summary>
    public static async Task<AcceptanceResult> EvaluateAsync(
        IAcceptanceEvaluator? evaluator,
        AcceptanceContext context,
        IApprovalGate? gate,
        TimeSpan? approvalTimeout = null,
        CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var verdict = (evaluator ?? DefaultAcceptanceEvaluator.Instance).Evaluate(context);
        if (verdict.Decision != AcceptanceDecision.Rollback || !RequiresHumanReview(verdict))
            return verdict;

        var approval = await GenerativeHumanReview.RouteAsync(
            context.Provenance,
            gate,
            verdict.Reason,
            approvalTimeout ?? DefaultApprovalTimeout,
            cancellationToken).ConfigureAwait(false);

        return approval == ApprovalResult.Approved
            ? AcceptanceResult.Ship(
                "approved by human review: " + verdict.Reason,
                Append(verdict.Signals, "human-approved"))
            : new AcceptanceResult(
                AcceptanceDecision.Rollback,
                $"human review {approval.ToString().ToLowerInvariant()}: {verdict.Reason}",
                Append(verdict.Signals, "human-" + approval.ToString().ToLowerInvariant()));
    }

    /// <summary>True when the verdict is waiting on a human rather than on evidence.</summary>
    public static bool RequiresHumanReview(AcceptanceResult verdict)
    {
        if (verdict is null) throw new ArgumentNullException(nameof(verdict));
        return verdict.Signals.Contains(DefaultAcceptanceEvaluator.HumanReviewSignal, StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> Append(IReadOnlyList<string> signals, string extra) =>
        signals.Concat(new[] { extra }).ToArray();
}
