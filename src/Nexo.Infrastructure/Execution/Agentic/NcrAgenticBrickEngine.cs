using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Execution.Agentic;

/// <summary>
/// Adapter connecting agentic brick execution to the node capability runtime.
/// </summary>
public sealed class NcrAgenticBrickEngine : IAgenticBrickEngine
{
    private readonly INodeCapabilityRuntime _ncr;

    /// <summary>Initializes a new ncr agentic brick engine.</summary>
    public NcrAgenticBrickEngine(INodeCapabilityRuntime ncr)
    {
        _ncr = ncr ?? throw new ArgumentNullException(nameof(ncr));
    }

    /// <summary>Resolve model for brick asynchronously.</summary>
    public async Task<ModelResolution> ResolveModelForBrickAsync(
        DomainBrick brick,
        IExecutionContext context,
        CancellationToken ct = default)
    {
        var taskContext = MapBrickToTaskContext(brick, context);
        var resolution = await _ncr.SelectModelAsync(taskContext, ct).ConfigureAwait(false);
        if (resolution.Target == InferenceTarget.Local && !string.IsNullOrWhiteSpace(resolution.Model.Id))
        {
            await _ncr.EnsureModelReadyAsync(resolution.Model, ct).ConfigureAwait(false);
        }

        return resolution;
    }

    /// <summary>Record execution outcome asynchronously.</summary>
    public Task RecordExecutionOutcomeAsync(
        ModelResolution resolution,
        BrickExecutionOutcome outcome,
        CancellationToken ct = default)
        => _ncr.RecordOutcomeAsync(resolution, outcome, ct);

    internal static TaskContext MapBrickToTaskContext(DomainBrick brick, IExecutionContext context)
    {
        if (brick is null) throw new ArgumentNullException(nameof(brick));
        if (context is null) throw new ArgumentNullException(nameof(context));

        var requiredCapability = MapCapability(brick.Category, brick.Implementations.HasAgentic);
        var isUserFacing = context.Variables.TryGetValue("nexo:user_facing", out var userFacingObj) &&
                           userFacingObj is bool userFacing &&
                           userFacing;
        var quality = ParseEnumVariable(context.Variables, "nexo:quality", QualityRequirement.Medium);
        var latency = isUserFacing ? LatencyRequirement.Interactive : LatencyRequirement.Background;
        var privacy = (context.IsAirGapped || context.AuditMode)
            ? PrivacyBoundary.LocalOnly
            : ParseEnumVariable(context.Variables, "nexo:privacy", PrivacyBoundary.Standard);

        return new TaskContext
        {
            RequiredCapability = requiredCapability,
            Quality = quality,
            Latency = latency,
            Privacy = privacy,
            IsUserFacing = isUserFacing,
            IsDelegationTask = context.Variables.ContainsKey("nexo:delegation"),
            RequestedProvider = string.IsNullOrWhiteSpace(context.Provider) ? null : context.Provider
        };
    }

    private static TEnum ParseEnumVariable<TEnum>(
        IReadOnlyDictionary<string, object> variables,
        string key,
        TEnum fallback)
        where TEnum : struct, Enum
    {
        if (!variables.TryGetValue(key, out var value))
        {
            return fallback;
        }

        if (value is string text && Enum.TryParse(text, ignoreCase: true, out TEnum parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static TaskCapability MapCapability(BrickCategory category, bool hasAgentic)
    {
        return category switch
        {
            BrickCategory.Generation => hasAgentic ? TaskCapability.TextGeneration : TaskCapability.Classification,
            BrickCategory.Analysis => TaskCapability.Reasoning,
            BrickCategory.Security => TaskCapability.CodeGeneration,
            BrickCategory.Transform => TaskCapability.TextGeneration,
            BrickCategory.Control => TaskCapability.Classification,
            BrickCategory.Output => TaskCapability.TextGeneration,
            _ => TaskCapability.TextGeneration
        };
    }
}
