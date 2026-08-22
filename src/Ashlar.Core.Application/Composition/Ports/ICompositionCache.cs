using Ashlar.Core.Application.Composition.Models;

namespace Ashlar.Core.Application.Composition.Ports;

/// <summary>
/// Cache for successful compositions.
/// </summary>
public interface ICompositionCache
{
    Task<bool> TryGetAsync(string problemKey, CancellationToken cancellationToken = default);
    Task StoreAsync(ComposedAgent agent, bool validated, CancellationToken cancellationToken = default);
}
