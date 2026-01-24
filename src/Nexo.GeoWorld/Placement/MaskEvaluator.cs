using Local2 = Nexo.GeoVector.Geometry.Vector2;

namespace Nexo.GeoWorld.Placement;

public sealed class MaskEvaluator
{
    private readonly IReadOnlyList<IPlacementMask> _includes;
    private readonly IReadOnlyList<IPlacementMask> _excludes;

    public MaskEvaluator(IReadOnlyList<IPlacementMask>? includes = null, IReadOnlyList<IPlacementMask>? excludes = null)
    {
        _includes = includes ?? Array.Empty<IPlacementMask>();
        _excludes = excludes ?? Array.Empty<IPlacementMask>();
    }

    public bool IsAllowed(Local2 pointMeters)
    {
        for (var i = 0; i < _excludes.Count; i++)
        {
            if (_excludes[i].Contains(pointMeters)) return false;
        }

        if (_includes.Count == 0) return true;

        for (var i = 0; i < _includes.Count; i++)
        {
            if (_includes[i].Contains(pointMeters)) return true;
        }

        return false;
    }
}

