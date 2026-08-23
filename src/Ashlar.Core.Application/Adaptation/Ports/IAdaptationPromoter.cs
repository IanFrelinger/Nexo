using Ashlar.Core.Application.Adaptation.Models;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Promotes validated fixes to core. Records that a fix was validated and promoted.
/// </summary>
public interface IAdaptationPromoter
{
    /// <summary>
    /// Promotes a validated adaptation record to the active core.
    /// </summary>
    /// <param name="record">Adaptation record that passed regression.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task PromoteAsync(AdaptationRecord record, CancellationToken cancellationToken = default);
}
