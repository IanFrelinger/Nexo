namespace Nexo.GeoTerrain;

/// <summary>
/// A regular grid of elevation samples in meters.
/// Missing samples are represented by <see cref="float.NaN"/>.
/// </summary>
public sealed class ElevationGrid
{
    private readonly float[] _heightsMeters;

    public int Width { get; }
    public int Height { get; }
    public GeoBounds Bounds { get; }
    public GridSpacing Spacing { get; }

    public ElevationGrid(int width, int height, GeoBounds bounds, GridSpacing spacing, float[] heightsMeters)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));
        ArgumentNullException.ThrowIfNull(bounds, nameof(bounds));
        ArgumentNullException.ThrowIfNull(heightsMeters, nameof(heightsMeters));
#else
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (bounds is null) throw new ArgumentNullException(nameof(bounds));
        if (heightsMeters is null) throw new ArgumentNullException(nameof(heightsMeters));
#endif
        bounds.Validate();
        if (heightsMeters.Length != checked(width * height))
            throw new ArgumentException("Heights array length must equal width*height.", nameof(heightsMeters));

        Width = width;
        Height = height;
        Bounds = bounds;
        Spacing = spacing;
        _heightsMeters = heightsMeters;
    }

    public float GetHeightMeters(int x, int y) => _heightsMeters[ToIndex(x, y)];

    public void SetHeightMeters(int x, int y, float meters) => _heightsMeters[ToIndex(x, y)] = meters;

    public bool IsNoData(int x, int y) => float.IsNaN(GetHeightMeters(x, y));

    public float[] CloneHeightsMeters() => (float[])_heightsMeters.Clone();

    private int ToIndex(int x, int y)
    {
        if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
        return checked(y * Width + x);
    }
}

