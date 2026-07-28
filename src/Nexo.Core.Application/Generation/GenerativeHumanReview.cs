using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Application.Generation;

/// <summary>
/// Routes <see cref="GenerativeProvenance.RequiresHumanReview"/> through
/// the existing <see cref="IApprovalGate"/> port.
/// </summary>
public static class GenerativeHumanReview
{
    /// <summary>
    /// Returns <see cref="ApprovalResult.Approved"/> when review is not required.
    /// When required and <paramref name="gate"/> is null, returns
    /// <see cref="ApprovalResult.Denied"/>.
    /// </summary>
    public static Task<ApprovalResult> RouteAsync(
        GenerativeProvenance provenance,
        IApprovalGate? gate,
        string actionDescription,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (provenance is null) throw new ArgumentNullException(nameof(provenance)); // ThrowIfNull unavailable on netstandard2.0
        if (!provenance.RequiresHumanReview)
            return Task.FromResult(ApprovalResult.Approved);

        if (gate is null)
            return Task.FromResult(ApprovalResult.Denied);

        return gate.RequestApprovalAsync(actionDescription, timeout, cancellationToken);
    }

    /// <summary>
    /// Adapter for <see cref="GenerativeBrick.RouteHumanReviewIfRequiredAsync"/>:
    /// maps <see cref="ApprovalResult.Approved"/> to true.
    /// </summary>
    public static Func<string, TimeSpan, CancellationToken, Task<bool>> Bind(IApprovalGate gate) =>
        async (description, timeout, ct) =>
            await gate.RequestApprovalAsync(description, timeout, ct).ConfigureAwait(false)
            == ApprovalResult.Approved;
}
