using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Promotes validated fixes. Records to adaptation log with Promoted=true.
/// </summary>
public sealed class AdaptationPromoter : IAdaptationPromoter
{
    private readonly IAdaptationLog _log;

    public AdaptationPromoter(IAdaptationLog log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <inheritdoc />
    public Task PromoteAsync(AdaptationRecord record, CancellationToken cancellationToken = default)
    {
        var promoted = record with { Promoted = true };
        return _log.LogAsync(promoted, cancellationToken);
    }
}
