using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Log of adaptation attempts. Records what was fixed, regression result, promoted or not.
/// </summary>
public interface IAdaptationLog
{
    Task LogAsync(AdaptationRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdaptationRecord>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, string? brickId = null, CancellationToken cancellationToken = default);
}
