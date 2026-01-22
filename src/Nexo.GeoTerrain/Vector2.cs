namespace Nexo.GeoTerrain;

/// <summary>
/// Minimal 2D vector type for portability (Unity/netstandard friendly).
/// Used for texture coordinates (UVs) and other planar data.
/// </summary>
public readonly struct Vector2 : IEquatable<Vector2>
{
    public float X { get; }
    public float Y { get; }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(Vector2 other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override bool Equals(object? obj) => obj is Vector2 other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + X.GetHashCode();
            hash = (hash * 31) + Y.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);
    public static bool operator !=(Vector2 left, Vector2 right) => !left.Equals(right);
}

