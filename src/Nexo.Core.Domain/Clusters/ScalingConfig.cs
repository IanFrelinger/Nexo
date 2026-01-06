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

/// <summary>
/// Scaling mode for cluster instances.
/// </summary>
public enum ScalingMode
{
    Single,      // One instance
    Fixed,       // Fixed number of instances
    Dynamic,     // Calculate at runtime based on expression
    EventDriven  // Spawn/destroy based on events
}

/// <summary>
/// How to distribute parameter variations across instances.
/// </summary>
public class InstanceDistribution
{
    /// <summary>
    /// Parameters to vary across instances.
    /// </summary>
    public IReadOnlyList<DistributionRule> Rules { get; init; } = [];
}

/// <summary>
/// Rule for distributing a parameter across instances.
/// </summary>
public class DistributionRule
{
    /// <summary>
    /// Parameter name to vary.
    /// </summary>
    public string Parameter { get; init; } = default!;
    
    /// <summary>
    /// Distribution type.
    /// </summary>
    public DistributionType Type { get; init; }
    
    /// <summary>
    /// For Enum distribution: values to cycle through.
    /// </summary>
    public IReadOnlyList<object>? Values { get; init; }
    
    /// <summary>
    /// For Range distribution: min value.
    /// </summary>
    public object? RangeMin { get; init; }
    
    /// <summary>
    /// For Range distribution: max value.
    /// </summary>
    public object? RangeMax { get; init; }
}

/// <summary>
/// Distribution type for parameter variation.
/// </summary>
public enum DistributionType
{
    Enum,           // Cycle through provided values
    Range,          // Random within range
    Sequential,     // Increment (for IDs, indices)
    WeightedRandom  // Random with weights
}

