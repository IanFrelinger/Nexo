namespace Nexo.GeoTerrain.Parsing;

/// <summary>
/// Parser for ASCII Grid format (ESRI ASCII Grid).
/// Format specification: https://en.wikipedia.org/wiki/Esri_grid
/// </summary>
public static class AsciiGridParser
{
    private static readonly char[] LineSeparators = { '\r', '\n' };
    private static readonly char[] FieldSeparators = { ' ', '\t' };

    /// <summary>
    /// Parses an ASCII Grid file into an ElevationGrid.
    /// </summary>
    public static ElevationGrid Parse(string text, GeoBounds? boundsOverride = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(text);
#else
        if (text is null) throw new ArgumentNullException(nameof(text));
#endif

        var lines = text.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 6)
            throw new InvalidOperationException("ASCII Grid must have at least 6 header lines.");

        var header = ParseHeader(lines);
        var data = ParseData(lines, header);

        var gridBounds = boundsOverride ?? header.Bounds;
        var spacing = new GridSpacing(header.CellSize, header.CellSize);

        return new ElevationGrid(header.NCols, header.NRows, gridBounds, spacing, data);
    }

    private static AsciiGridHeader ParseHeader(string[] lines)
    {
        var header = new AsciiGridHeader();

        for (var i = 0; i < Math.Min(6, lines.Length); i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(FieldSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            var key = parts[0].Trim().ToUpperInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "NCOLS":
                    header.NCols = int.Parse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "NROWS":
                    header.NRows = int.Parse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "XLLCORNER":
                case "XLLCENTER":
                    header.Xll = double.Parse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
#if NET8_0_OR_GREATER
                    header.UseCorner = key.Contains("CORNER", StringComparison.OrdinalIgnoreCase);
#else
                    header.UseCorner = key.IndexOf("CORNER", StringComparison.OrdinalIgnoreCase) >= 0;
#endif
                    break;
                case "YLLCORNER":
                case "YLLCENTER":
                    header.Yll = double.Parse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "CELLSIZE":
                case "CELL_SIZE":
                    header.CellSize = double.Parse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "NODATA_VALUE":
                case "NODATA":
                    header.NoDataValue = double.Parse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                    break;
            }
        }

        if (header.NCols <= 0 || header.NRows <= 0)
            throw new InvalidOperationException("ASCII Grid must have valid NCOLS and NROWS.");

        if (header.CellSize <= 0)
            throw new InvalidOperationException("ASCII Grid must have valid CELLSIZE.");

        // Compute bounds from header
        var halfCell = header.CellSize * 0.5;
        var minLon = header.Xll;
        var maxLon = header.Xll + (header.NCols * header.CellSize);
        var minLat = header.Yll;
        var maxLat = header.Yll + (header.NRows * header.CellSize);

        header.Bounds = new GeoBounds
        {
            MinLongitude = new Longitude(minLon),
            MaxLongitude = new Longitude(maxLon),
            MinLatitude = new Latitude(minLat),
            MaxLatitude = new Latitude(maxLat)
        };

        return header;
    }

    private static float[] ParseData(string[] lines, AsciiGridHeader header)
    {
        // Find first data line (after header)
        var dataStart = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var firstWord = line.Split(FieldSeparators, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (firstWord != null && double.TryParse(firstWord, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                dataStart = i;
                break;
            }
        }

        var heights = new float[header.NCols * header.NRows];
        var hi = 0;

        for (var i = dataStart; i < lines.Length && hi < heights.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = line.Split(FieldSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var valStr in values)
            {
                if (hi >= heights.Length) break;

                if (double.TryParse(valStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var val))
                {
                    if (Math.Abs(val - header.NoDataValue) < 0.0001)
                        heights[hi++] = float.NaN;
                    else
                        heights[hi++] = (float)val;
                }
                else
                {
                    heights[hi++] = float.NaN;
                }
            }
        }

        if (hi < heights.Length)
            throw new InvalidOperationException($"ASCII Grid data incomplete: expected {heights.Length} values, got {hi}.");

        return heights;
    }

    private sealed class AsciiGridHeader
    {
        public int NCols { get; set; }
        public int NRows { get; set; }
        public double Xll { get; set; }
        public double Yll { get; set; }
        public double CellSize { get; set; }
        public double NoDataValue { get; set; } = -9999.0;
        public bool UseCorner { get; set; } = true;
        public GeoBounds Bounds { get; set; } = new GeoBounds
        {
            MinLatitude = new Latitude(0),
            MaxLatitude = new Latitude(1),
            MinLongitude = new Longitude(0),
            MaxLongitude = new Longitude(1)
        };
    }
}
