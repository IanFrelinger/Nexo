namespace Nexo.GeoTerrain;

/// <summary>
/// Parser for NASA SRTM .hgt tiles (big-endian signed 16-bit samples).
/// </summary>
public static class SrtmHgtParser
{
    public const short NoDataSentinel = -32768;

    public static SrtmHgtSummary Analyze(byte[] data)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(data);
#else
        if (data is null) throw new ArgumentNullException(nameof(data));
#endif
        if ((data.Length % 2) != 0) throw new ArgumentException("HGT data length must be even (16-bit samples).", nameof(data));

        var samples = data.Length / 2;
        var size = IntegerSqrt(samples);
        if (size * size != samples)
            throw new ArgumentException("HGT sample count must be a perfect square.", nameof(data));

        var nodata = 0;
        var min = int.MaxValue;
        var max = int.MinValue;

        for (var i = 0; i < data.Length; i += 2)
        {
            var v = ReadInt16BigEndian(data, i);
            if (v == NoDataSentinel)
            {
                nodata++;
                continue;
            }
            if (v < min) min = v;
            if (v > max) max = v;
        }

        if (min == int.MaxValue) min = 0;
        if (max == int.MinValue) max = 0;

        return new SrtmHgtSummary
        {
            Size = size,
            SampleCount = samples,
            MinMeters = (short)min,
            MaxMeters = (short)max,
            NoDataSamples = nodata
        };
    }

    /// <summary>
    /// Parses the tile into a row-major height array in meters. NoData samples become NaN.
    /// </summary>
    public static float[] ParseHeightsMeters(byte[] data, out int size)
    {
        var summary = Analyze(data);
        size = summary.Size;

        var heights = new float[summary.SampleCount];
        var si = 0;
        for (var i = 0; i < data.Length; i += 2)
        {
            var v = ReadInt16BigEndian(data, i);
            heights[si++] = v == NoDataSentinel ? float.NaN : v;
        }
        return heights;
    }

    private static short ReadInt16BigEndian(byte[] data, int offset)
    {
        unchecked
        {
            return (short)((data[offset] << 8) | data[offset + 1]);
        }
    }

    private static int IntegerSqrt(int n)
    {
        // integer sqrt via Math.Sqrt is fine here (n is <= ~13M for 3601^2).
        return (int)Math.Round(Math.Sqrt(n));
    }
}

public sealed record SrtmHgtSummary
{
    public required int Size { get; init; }
    public required int SampleCount { get; init; }
    public required short MinMeters { get; init; }
    public required short MaxMeters { get; init; }
    public required int NoDataSamples { get; init; }
}

