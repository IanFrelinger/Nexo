namespace Nexo.GeoTerrain;

/// <summary>
/// Longitude in degrees, constrained to [-180, 180].
/// </summary>
public readonly struct Longitude : IEquatable<Longitude>
{
    public double Degrees { get; }

    public Longitude(double degrees)
    {
        if (double.IsNaN(degrees) || double.IsInfinity(degrees))
            throw new ArgumentOutOfRangeException(nameof(degrees), "Longitude must be a finite number.");
        if (degrees < -180d || degrees > 180d)
            throw new ArgumentOutOfRangeException(nameof(degrees), "Longitude must be within [-180, 180] degrees.");

        Degrees = degrees;
    }

    public bool Equals(Longitude other) => Degrees.Equals(other.Degrees);
    public override bool Equals(object? obj) => obj is Longitude other && Equals(other);
    public override int GetHashCode() => Degrees.GetHashCode();
    public override string ToString() => Degrees.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Longitude left, Longitude right) => left.Equals(right);
    public static bool operator !=(Longitude left, Longitude right) => !left.Equals(right);
}
