using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Scoring;

/// <summary>
/// Scores models for the current task and node profile.
/// </summary>
public sealed class ModelScoringService
{
    private const long LargeModelThresholdBytes = 7L * 1024 * 1024 * 1024;
    private readonly IPlatformPolicy _policy;

    public ModelScoringService(IPlatformPolicy policy)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public float ScoreModel(ModelDescriptor model, TaskContext context, NodeProfile profile)
    {
        if (!_policy.CanLoadModel(model, profile)) return float.MinValue;
        if (!model.Capabilities.Contains(context.RequiredCapability)) return float.MinValue;
        if (!model.SupportedPlatforms.Contains(profile.Platform)) return float.MinValue;
        if (context.Privacy == PrivacyBoundary.LocalOnly && model.RequiresRemote) return float.MinValue;

        var qualityWeight = context.Latency switch
        {
            LatencyRequirement.RealTime => 0.2f,
            LatencyRequirement.Interactive => 0.4f,
            LatencyRequirement.Background => 0.7f,
            LatencyRequirement.Overnight => 0.9f,
            _ => 0.5f
        };
        var speedWeight = 1.0f - qualityWeight;

        var memoryPressure = profile.TotalVRAMBytes > 0
            ? 1.0f - (float)profile.AvailableVRAMBytes / profile.TotalVRAMBytes
            : profile.TotalRAMBytes > 0
                ? 1.0f - (float)profile.AvailableRAMBytes / profile.TotalRAMBytes
                : 1.0f;

        var resourcePenalty = memoryPressure * 0.3f;
        var loadPenalty = model.State switch
        {
            ModelState.Hot => 0.0f,
            ModelState.Warm => 0.1f,
            _ => 0.3f
        };

        var batteryPenalty = (profile.IsOnBattery && !profile.IsCharging && model.WeightSizeBytes > LargeModelThresholdBytes)
            ? 0.5f
            : 0.0f;

        return (qualityWeight * model.QualityScore)
               + (speedWeight * model.SpeedScore)
               - resourcePenalty
               - loadPenalty
               - batteryPenalty;
    }
}
