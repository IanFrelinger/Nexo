using Local2 = Nexo.GeoVector.Geometry.Vector2;

namespace Nexo.GeoWorld.Placement;

public interface IPlacementMask
{
    bool Contains(Local2 pointMeters);
}

