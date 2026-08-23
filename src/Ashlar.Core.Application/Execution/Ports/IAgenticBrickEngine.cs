using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Ports;

/// <summary>
/// Bridge between agentic brick execution and NCR model selection.
/// </summary>
public interface IAgenticBrickEngine
{
    /// <summary>
    /// Resolves which model/provider should back this brick execution.
    /// </summary>
    Task<ModelResolution> ResolveModelForBrickAsync(
        DomainBrick brick,
        IExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Records execution outcome as a scoring feedback signal.
    /// </summary>
    Task RecordExecutionOutcomeAsync(
        ModelResolution resolution,
        BrickExecutionOutcome outcome,
        CancellationToken ct = default);
}
