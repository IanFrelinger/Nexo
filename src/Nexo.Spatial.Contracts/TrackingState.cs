namespace Nexo.Spatial.Contracts;

/// <summary>
/// Tracking quality reported by a spatial anchor provider.
/// </summary>
public enum TrackingState
{
    Tracking,
    Occluded,
    Lost
}
