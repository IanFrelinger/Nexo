namespace Nexo.Core.Domain.Clusters;

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
