using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Domain.Behaviors;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Rewires behaviors by applying step order, mapping overrides, and brick swaps.
/// </summary>
public sealed class BehaviorRewirer : IBehaviorRewirer
{
    /// <inheritdoc />
    public Task<Behavior> RewireAsync(
        Behavior behavior,
        BehaviorRewireChanges changes,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var steps = behavior.Steps.ToList();

        if (changes.StepOrder != null && changes.StepOrder.Count == steps.Count)
        {
            var byId = steps.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
            steps = changes.StepOrder
                .Select(id => byId.TryGetValue(id, out var s) ? s : null)
                .Where(s => s != null)
                .Cast<BehaviorStep>()
                .ToList();
        }

        var brickSwaps = changes.BrickSwaps ?? new Dictionary<string, string>();
        var inputOverrides = changes.InputMappingOverrides ?? new Dictionary<string, IReadOnlyDictionary<string, string>>();
        var outputOverrides = changes.OutputMappingOverrides ?? new Dictionary<string, IReadOnlyDictionary<string, string>>();

        var newSteps = steps.Select(step =>
        {
            var brickId = brickSwaps.TryGetValue(step.Id, out var swapped) ? swapped : step.BrickId;
            var inputMapping = inputOverrides.TryGetValue(step.Id, out var im) ? im : step.InputMapping;
            var outputMapping = outputOverrides.TryGetValue(step.Id, out var om) ? om : step.OutputMapping;

            if (brickId == step.BrickId && inputMapping == step.InputMapping && outputMapping == step.OutputMapping)
                return step;

            return new BehaviorStep
            {
                Id = step.Id,
                BrickId = brickId,
                Implementation = step.Implementation,
                InputMapping = new Dictionary<string, string>(inputMapping),
                OutputMapping = new Dictionary<string, string>(outputMapping),
                Condition = step.Condition,
                Config = step.Config,
            };
        }).ToList();

        var result = new Behavior
        {
            Id = behavior.Id,
            Name = behavior.Name,
            Version = behavior.Version,
            Description = behavior.Description,
            Steps = newSteps,
            OnStepFailure = behavior.OnStepFailure,
            MaxRetries = behavior.MaxRetries,
            Timeout = behavior.Timeout,
            SuccessCriteria = behavior.SuccessCriteria,
            Inputs = behavior.Inputs,
            Outputs = behavior.Outputs,
        };

        return Task.FromResult(result);
    }
}
