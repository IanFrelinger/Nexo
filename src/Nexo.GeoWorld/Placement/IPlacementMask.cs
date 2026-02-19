using Local2 = Nexo.GeoVector.Geometry.Vector2;

namespace Nexo.GeoWorld.Placement;

/// <summary>
/// Determines whether a 2D point (in meters) is inside a placement region.
/// Used for vegetation, object placement, and spatial filtering.
/// </summary>
public interface IPlacementMask
{
    /// <summary>Returns true if the point is within the mask region.</summary>
    /// <param name="pointMeters">Point in local meters (x, y).</param>
    bool Contains(Local2 pointMeters);
}

