namespace Ashlar.Core.Domain.Workflows;

/// <summary>Direction of a workflow node port for visual composer connections.</summary>
public enum PortDirection
{
    /// <summary>Port receives data from upstream nodes.</summary>
    Input,

    /// <summary>Port emits data to downstream nodes.</summary>
    Output
}
