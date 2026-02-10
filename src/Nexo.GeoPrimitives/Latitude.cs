namespace Nexo.GeoTerrain;

/// <summary>
/// Latitude in degrees, constrained to [-90, 90].
/// </summary>
public readonly struct Latitude : IEquatable<Latitude>
{
    public double Degrees { get; }

    public Latitude(double degrees)
    {
        if (double.IsNaN(degrees) || double.IsInfinity(degrees))
            throw new ArgumentOutOfRangeException(nameof(degrees), "Latitude must be a finite number.");
        if (degrees < -90d || degrees > 90d)
            throw new ArgumentOutOfRangeException(nameof(degrees), "Latitude must be within [-90, 90] degrees.");

        Degrees = degrees;
    }

    public bool Equals(Latitude other) => Degrees.Equals(other.Degrees);
    public override bool Equals(object? obj) => obj is Latitude other && Equals(other);
    public override int GetHashCode() => Degrees.GetHashCode();
    public override string ToString() => Degrees.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Latitude left, Latitude right) => left.Equals(right);
    public static bool operator !=(Latitude left, Latitude right) => !left.Equals(right);
}
