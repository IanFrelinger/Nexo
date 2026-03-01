using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Promotes validated fixes to core. Records that a fix was validated and promoted.
/// </summary>
public interface IAdaptationPromoter
{
    Task PromoteAsync(AdaptationRecord record, CancellationToken cancellationToken = default);
}
