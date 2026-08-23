namespace Ashlar.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// On-device storage availability for model artifacts.
/// </summary>
public sealed record StorageProfile
{
    /// <summary>Free storage bytes available for model downloads.</summary>
    public long AvailableBytes { get; init; }

    /// <summary>Total storage capacity on the device.</summary>
    public long TotalBytes { get; init; }
}
