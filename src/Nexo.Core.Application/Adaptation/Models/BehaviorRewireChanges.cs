using Nexo.Core.Domain.Behaviors;

namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Describes changes to apply when rewiring a behavior.
/// </summary>
public record BehaviorRewireChanges
{
    /// <summary>New step order (step IDs).</summary>
    public IReadOnlyList<string>? StepOrder { get; init; }

    /// <summary>Per-step input mapping overrides.</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? InputMappingOverrides { get; init; }

    /// <summary>Per-step output mapping overrides.</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? OutputMappingOverrides { get; init; }

    /// <summary>Brick ID swaps: stepId -> newBrickId.</summary>
    public IReadOnlyDictionary<string, string>? BrickSwaps { get; init; }
}
