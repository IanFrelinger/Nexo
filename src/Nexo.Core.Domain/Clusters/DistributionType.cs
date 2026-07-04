namespace Nexo.Core.Domain.Clusters;

/// <summary>Strategy for varying parameter values across scaled cluster instances.</summary>
public enum DistributionType
{
    /// <summary>Cycle through a fixed set of provided values.</summary>
    Enum,

    /// <summary>Sample random values within a configured range.</summary>
    Range,

    /// <summary>Increment a numeric sequence across instances.</summary>
    Sequential,

    /// <summary>Random selection using explicit weights.</summary>
    WeightedRandom
}
