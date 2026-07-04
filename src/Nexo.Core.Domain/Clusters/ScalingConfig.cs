namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// How this cluster scales to multiple instances.
/// </summary>
public class ScalingConfig
{
    /// <summary>
    /// Scaling mode.
    /// </summary>
    public ScalingMode Mode { get; init; } = ScalingMode.Single;
    
    /// <summary>
    /// For Fixed mode: exact number of instances.
    /// </summary>
    public int FixedCount { get; init; } = 1;
    
    /// <summary>
    /// For Dynamic mode: expression to calculate instance count.
    /// Examples: "levelSize / 100", "playerCount * 2", "config.enemyDensity"
    /// </summary>
    public string? DynamicExpression { get; init; }
    
    /// <summary>
    /// For EventDriven mode: event that triggers instance creation.
    /// </summary>
    public string? TriggerEvent { get; init; }
    
    /// <summary>
    /// Maximum instances regardless of mode.
    /// </summary>
    public int MaxInstances { get; init; } = 100;
    
    /// <summary>
    /// Minimum instances regardless of mode.
    /// </summary>
    public int MinInstances { get; init; } = 0;
    
    /// <summary>
    /// How to distribute parameter variations across instances.
    /// </summary>
    public InstanceDistribution? Distribution { get; init; }
}
