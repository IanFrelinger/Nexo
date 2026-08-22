using Ashlar.Core.Application.Execution.Ports;
using Ashlar.Core.Application.NodeCapabilityRuntime.Models;

namespace Ashlar.Core.Application.NodeCapabilityRuntime.Ports;

/// <summary>
/// Runtime facade for node capability profiling, model selection, and inference lifecycle.
/// </summary>
public interface INodeCapabilityRuntime
{
    /// <summary>Current hardware and platform profile snapshot.</summary>
    NodeProfile CurrentProfile { get; }

    /// <summary>Selects the best model for a task given current constraints.</summary>
    Task<ModelResolution> SelectModelAsync(TaskContext context, CancellationToken ct = default);

    /// <summary>Ensures the selected model is loaded and ready for inference.</summary>
    Task EnsureModelReadyAsync(ModelDescriptor model, CancellationToken ct = default);

    /// <summary>Returns the compute tier of this node.</summary>
    Task<NodeTier> GetTierAsync();

    /// <summary>Stream of constraint updates (battery, thermal, network) affecting routing.</summary>
    IObservable<ConstraintUpdate> ConstraintChanges { get; }

    /// <summary>Models currently available on this node.</summary>
    IReadOnlyList<ModelDescriptor> AvailableModels { get; }

    /// <summary>Publishes a wire-format capability manifest for federation routing.</summary>
    NodeCapabilityManifest GetCapabilityManifest();

    /// <summary>Records execution outcome feedback to improve future model selection.</summary>
    Task RecordOutcomeAsync(
        ModelResolution resolution,
        BrickExecutionOutcome outcome,
        CancellationToken ct = default);
}
