using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Domain.Behaviors;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Modifies DAG connections in a behavior without full rebuild.
/// </summary>
public interface IBehaviorRewirer
{
    /// <summary>
    /// Rewire a behavior: change step order, input/output mappings, or swap bricks.
    /// </summary>
    /// <param name="behavior">The behavior to rewire.</param>
    /// <param name="changes">Description of changes (e.g. reorder steps, update mappings).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Behavior> RewireAsync(
        Behavior behavior,
        BehaviorRewireChanges changes,
        CancellationToken cancellationToken = default);
}
