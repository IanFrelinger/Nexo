using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Domain.Behaviors;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Assembles new behaviors from existing bricks.
/// </summary>
public interface INewBehaviorAssembler
{
    /// <summary>
    /// Assemble a new behavior from bricks to accomplish the given goal.
    /// </summary>
    /// <param name="goal">Description of what the behavior should do.</param>
    /// <param name="availableBrickIds">Brick IDs that can be used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Behavior> AssembleAsync(
        string goal,
        IReadOnlyList<string> availableBrickIds,
        CancellationToken cancellationToken = default);
}
