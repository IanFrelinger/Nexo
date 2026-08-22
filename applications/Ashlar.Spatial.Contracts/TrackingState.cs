namespace Ashlar.Spatial.Contracts;

/// <summary>
/// Tracking quality reported by a spatial anchor provider.
/// </summary>
public enum TrackingState
{
    /// <summary>Pose is actively tracked with sufficient quality.</summary>
    Tracking,

    /// <summary>Pose is temporarily degraded (occlusion, low confidence) but not fully lost.</summary>
    Occluded,

    /// <summary>Pose tracking has been lost; consumers should not rely on the last sample.</summary>
    Lost
}
