namespace Nexo.Core.Domain.Clusters;

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
