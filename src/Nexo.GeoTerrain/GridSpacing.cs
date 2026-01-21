namespace Nexo.GeoTerrain;

/// <summary>
/// Spacing between samples in a projected/local coordinate space (meters).
/// </summary>
public readonly struct GridSpacing : IEquatable<GridSpacing>
{
    public double MetersX { get; }
    public double MetersY { get; }

    public GridSpacing(double metersX, double metersY)
    {
        if (double.IsNaN(metersX) || double.IsInfinity(metersX) || metersX <= 0)
            throw new ArgumentOutOfRangeException(nameof(metersX), "MetersX must be a positive finite number.");
        if (double.IsNaN(metersY) || double.IsInfinity(metersY) || metersY <= 0)
            throw new ArgumentOutOfRangeException(nameof(metersY), "MetersY must be a positive finite number.");

        MetersX = metersX;
        MetersY = metersY;
    }

    public bool Equals(GridSpacing other) => MetersX.Equals(other.MetersX) && MetersY.Equals(other.MetersY);
    public override bool Equals(object? obj) => obj is GridSpacing other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + MetersX.GetHashCode();
            hash = (hash * 31) + MetersY.GetHashCode();
            return hash;
        }
    }
    public static bool operator ==(GridSpacing left, GridSpacing right) => left.Equals(right);
    public static bool operator !=(GridSpacing left, GridSpacing right) => !left.Equals(right);
}

