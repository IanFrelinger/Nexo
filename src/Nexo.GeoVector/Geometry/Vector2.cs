namespace Nexo.GeoVector.Geometry;

/// <summary>
/// Minimal 2D vector (local/projected coordinates, typically meters).
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

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, float s) => new(a.X * s, a.Y * s);

    // Friendly alternates for operator overloads (analyzer compliance).
    public static Vector2 Add(Vector2 a, Vector2 b) => a + b;
    public static Vector2 Subtract(Vector2 a, Vector2 b) => a - b;
    public static Vector2 Multiply(Vector2 a, float s) => a * s;

    public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);
    public static bool operator !=(Vector2 left, Vector2 right) => !left.Equals(right);

    public bool Equals(Vector2 other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override bool Equals(object? obj) => obj is Vector2 v && Equals(v);

    public override int GetHashCode()
    {
        unchecked
        {
            var h = 17;
            h = (h * 31) + X.GetHashCode();
            h = (h * 31) + Y.GetHashCode();
            return h;
        }
    }

    public override string ToString() => $"({X}, {Y})";
}

