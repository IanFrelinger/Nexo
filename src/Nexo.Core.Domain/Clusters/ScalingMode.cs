namespace Nexo.Core.Domain.Clusters;

/// <summary>How many instances of a cluster are materialized at runtime.</summary>
public enum ScalingMode
{
    /// <summary>Exactly one cluster instance.</summary>
    Single,

    /// <summary>Fixed number of instances configured up front.</summary>
    Fixed,

    /// <summary>Instance count computed from a runtime expression.</summary>
    Dynamic,

    /// <summary>Instances created or destroyed in response to events.</summary>
    EventDriven
}
