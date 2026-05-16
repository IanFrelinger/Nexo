using System.Globalization;

namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Extracts Web Mercator tile indices (z/x/y) from common vector-tile URL layouts (standard z/x/y path or query).
/// </summary>
public static class VectorTileUrlParser
{
    /// <summary>
    /// Attempts to parse z/x/y from the URL path (three consecutive integers with valid tile bounds) or query (?z=&amp;x=&amp;y=).
    /// </summary>
    public static bool TryParseTile(string? rawUrl, out int z, out int x, out int y)
    {
        z = 0;
        x = 0;
        y = 0;
        if (string.IsNullOrWhiteSpace(rawUrl))
            return false;

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri))
            return false;

        if (TryParseFromQuery(uri.Query, out z, out x, out y))
            return true;

        return TryParseFromPath(uri.AbsolutePath, out z, out x, out y);
    }

    private static bool TryParseFromQuery(string query, out int z, out int x, out int y)
    {
        z = 0;
        x = 0;
        y = 0;
        if (string.IsNullOrEmpty(query) || query.Length < 2)
            return false;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.StartsWith('?') ? query[1..] : query;
        foreach (var segment in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = segment.IndexOf('=');
            if (eq <= 0)
                continue;
            var key = Uri.UnescapeDataString(segment[..eq]).Trim();
            var val = Uri.UnescapeDataString(segment[(eq + 1)..]).Trim();
            map[key] = val;
        }

        static bool PickZoom(IReadOnlyDictionary<string, string> m, out int zz)
        {
            zz = 0;
            foreach (var key in new[] { "z", "zoom", "Z" })
            {
                if (m.TryGetValue(key, out var s) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out zz))
                    return true;
            }

            return false;
        }

        if (!PickZoom(map, out z))
            return false;
        if (!map.TryGetValue("x", out var sx) || !int.TryParse(sx, NumberStyles.Integer, CultureInfo.InvariantCulture, out x))
        {
            if (!map.TryGetValue("tile_x", out sx) ||
                !int.TryParse(sx, NumberStyles.Integer, CultureInfo.InvariantCulture, out x))
                return false;
        }

        if (!map.TryGetValue("y", out var sy) || !int.TryParse(sy, NumberStyles.Integer, CultureInfo.InvariantCulture, out y))
        {
            if (!map.TryGetValue("tile_y", out sy) ||
                !int.TryParse(sy, NumberStyles.Integer, CultureInfo.InvariantCulture, out y))
                return false;
        }

        return IsValidTile(z, x, y);
    }

    private static bool TryParseFromPath(string absolutePath, out int z, out int x, out int y)
    {
        z = 0;
        x = 0;
        y = 0;
        if (string.IsNullOrEmpty(absolutePath))
            return false;

        var segments = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3)
            return false;

        for (var i = 0; i <= segments.Length - 3; i++)
        {
            if (!TryParseSegment(segments[i], out var tz))
                continue;
            if (!TryParseSegment(segments[i + 1], out var tx))
                continue;
            if (!TryParseSegment(segments[i + 2], out var ty))
                continue;

            if (!IsValidTile(tz, tx, ty))
                continue;

            z = tz;
            x = tx;
            y = ty;
            return true;
        }

        return false;
    }

    private static bool TryParseSegment(string segment, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(segment))
            return false;
        var dot = segment.IndexOf('.');
        var core = dot >= 0 ? segment[..dot] : segment;
        return int.TryParse(core, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool IsValidTile(int tz, int tx, int ty)
    {
        if (tz is < 0 or > 22)
            return false;
        var dim = 1 << tz;
        if ((uint)tx >= dim || (uint)ty >= dim)
            return false;
        return true;
    }
}
