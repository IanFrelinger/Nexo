namespace Nexo.GeoTerrain;

/// <summary>
/// Decoder for Mapbox Terrain-RGB elevations.
/// Formula: height(m) = -10000 + (R*256*256 + G*256 + B) * 0.1
/// </summary>
public static class TerrainRgbDecoder
{
    public static float DecodeMeters(byte r, byte g, byte b)
    {
        var v = (r * 256 * 256) + (g * 256) + b;
        return -10000f + (v * 0.1f);
    }
}

