namespace Nexo.GeoTerrain;

/// <summary>
/// Elevation unit as a value object (domain "no-enum" style).
/// </summary>
public sealed class ElevationUnit : IEquatable<ElevationUnit>
{
    public string Name { get; }
    public string Symbol { get; }
    public double MetersPerUnit { get; }

    private ElevationUnit(string name, string symbol, double metersPerUnit)
    {
        Name = name;
        Symbol = symbol;
        MetersPerUnit = metersPerUnit;
    }

    public static readonly ElevationUnit Meters = new("Meters", "m", 1.0);
    public static readonly ElevationUnit Feet = new("Feet", "ft", 0.3048);

    public static readonly IReadOnlyList<ElevationUnit> All = new[] { Meters, Feet };

    public static ElevationUnit FromName(string name) =>
        All.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Meters;

    public static ElevationUnit FromSymbol(string symbol) =>
        All.FirstOrDefault(u => u.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)) ?? Meters;

    public bool Equals(ElevationUnit? other) => other != null && MetersPerUnit.Equals(other.MetersPerUnit);
    public override bool Equals(object? obj) => obj is ElevationUnit other && Equals(other);
    public override int GetHashCode() => MetersPerUnit.GetHashCode();
    public override string ToString() => Symbol;

    public static bool operator ==(ElevationUnit? left, ElevationUnit? right) =>
        ReferenceEquals(left, right) || (left?.Equals(right) ?? false);

    public static bool operator !=(ElevationUnit? left, ElevationUnit? right) => !(left == right);
}

