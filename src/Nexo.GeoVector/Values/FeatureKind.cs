namespace Nexo.GeoVector.Values;

/// <summary>
/// Value-object identifier for a vector feature kind (no-enum style).
/// Examples: "building", "road", "vegetation".
/// </summary>
public sealed class FeatureKind : IEquatable<FeatureKind>
{
    public string Value { get; }

    public FeatureKind(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("FeatureKind cannot be null/empty.", nameof(value));
        Value = value.Trim();
    }

    public static FeatureKind Building { get; } = new("building");
    public static FeatureKind Road { get; } = new("road");
    public static FeatureKind Vegetation { get; } = new("vegetation");

    public bool Equals(FeatureKind? other) => other != null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    public override bool Equals(object? obj) => obj is FeatureKind k && Equals(k);
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    public override string ToString() => Value;
}

