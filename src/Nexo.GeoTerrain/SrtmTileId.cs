namespace Nexo.GeoTerrain;

/// <summary>
/// Identifies a 1x1 degree SRTM tile by its standard name, e.g. "N37W122".
/// </summary>
public readonly struct SrtmTileId : IEquatable<SrtmTileId>
{
    public int SouthLatitudeDegrees { get; }
    public int WestLongitudeDegrees { get; }

    public SrtmTileId(int southLatitudeDegrees, int westLongitudeDegrees)
    {
        if (southLatitudeDegrees < -90 || southLatitudeDegrees > 89)
            throw new ArgumentOutOfRangeException(nameof(southLatitudeDegrees));
        if (westLongitudeDegrees < -180 || westLongitudeDegrees > 179)
            throw new ArgumentOutOfRangeException(nameof(westLongitudeDegrees));

        SouthLatitudeDegrees = southLatitudeDegrees;
        WestLongitudeDegrees = westLongitudeDegrees;
    }

    public static bool TryParse(string? name, out SrtmTileId tile)
    {
        tile = default;
        if (name is null) return false;
        var s = name.Trim();
        if (s.Length == 0) return false;

        // Accept "N37W122" and also filenames like "N37W122.hgt"
        if (s.EndsWith(".hgt", StringComparison.OrdinalIgnoreCase))
        {
#if NET8_0_OR_GREATER
            s = s.AsSpan(0, s.Length - 4).ToString();
#else
            s = s.Substring(0, s.Length - 4);
#endif
        }

        if (s.Length != 7) return false;

        var ns = s[0];
        var ew = s[3];
        if (ns != 'N' && ns != 'S') return false;
        if (ew != 'E' && ew != 'W') return false;

#if NET8_0_OR_GREATER
        if (!int.TryParse(s.AsSpan(1, 2), out var latAbs)) return false;
        if (!int.TryParse(s.AsSpan(4, 3), out var lonAbs)) return false;
#else
        if (!int.TryParse(s.Substring(1, 2), out var latAbs)) return false;
        if (!int.TryParse(s.Substring(4, 3), out var lonAbs)) return false;
#endif

        var southLat = ns == 'N' ? latAbs : -latAbs;
        var westLon = ew == 'E' ? lonAbs : -lonAbs;

        tile = new SrtmTileId(southLat, westLon);
        return true;
    }

    public GeoBounds ToBounds()
    {
        var minLat = new Latitude(SouthLatitudeDegrees);
        var maxLat = new Latitude(SouthLatitudeDegrees + 1);
        var minLon = new Longitude(WestLongitudeDegrees);
        var maxLon = new Longitude(WestLongitudeDegrees + 1);

        return new GeoBounds
        {
            MinLatitude = minLat,
            MaxLatitude = maxLat,
            MinLongitude = minLon,
            MaxLongitude = maxLon
        };
    }

    public bool Equals(SrtmTileId other) =>
        SouthLatitudeDegrees == other.SouthLatitudeDegrees &&
        WestLongitudeDegrees == other.WestLongitudeDegrees;

    public override bool Equals(object? obj) => obj is SrtmTileId other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + SouthLatitudeDegrees.GetHashCode();
            hash = (hash * 31) + WestLongitudeDegrees.GetHashCode();
            return hash;
        }
    }

    public override string ToString()
    {
        var ns = SouthLatitudeDegrees >= 0 ? 'N' : 'S';
        var ew = WestLongitudeDegrees >= 0 ? 'E' : 'W';
        var latAbs = Math.Abs(SouthLatitudeDegrees);
        var lonAbs = Math.Abs(WestLongitudeDegrees);
        return $"{ns}{latAbs:00}{ew}{lonAbs:000}";
    }

    public static bool operator ==(SrtmTileId left, SrtmTileId right) => left.Equals(right);
    public static bool operator !=(SrtmTileId left, SrtmTileId right) => !left.Equals(right);
}

