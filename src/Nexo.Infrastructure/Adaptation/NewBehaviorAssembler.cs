using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Assembles new behaviors from bricks given a goal. Stub implementation.
/// </summary>
public sealed class NewBehaviorAssembler : INewBehaviorAssembler
{
    /// <inheritdoc />
    public Task<Behavior> AssembleAsync(
        string goal,
        IReadOnlyList<string> availableBrickIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var steps = availableBrickIds
            .Take(3)
            .Select((brickId, i) => new BehaviorStep
            {
                Id = $"step_{i + 1}",
                BrickId = brickId,
                Implementation = ImplementationType.Auto,
                InputMapping = new Dictionary<string, string>(),
                OutputMapping = new Dictionary<string, string>(),
            })
            .ToList();

        var behavior = new Behavior
        {
            Id = $"assembled.{Guid.NewGuid():N}",
            Name = $"Assembled for: {goal[..Math.Min(50, goal.Length)]}",
            Version = "1.0.0",
            Description = goal,
            Steps = steps,
            Inputs = [],
            Outputs = [],
        };

        return Task.FromResult(behavior);
    }
}
