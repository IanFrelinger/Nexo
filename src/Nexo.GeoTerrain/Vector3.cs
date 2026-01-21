namespace Nexo.GeoTerrain;

/// <summary>
/// Minimal 3D vector type for portability (Unity/netstandard friendly).
/// </summary>
public readonly struct Vector3 : IEquatable<Vector3>
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public bool Equals(Vector3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + X.GetHashCode();
            hash = (hash * 31) + Y.GetHashCode();
            hash = (hash * 31) + Z.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);
    public static bool operator !=(Vector3 left, Vector3 right) => !left.Equals(right);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator *(Vector3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);

    public static Vector3 Add(Vector3 a, Vector3 b) => a + b;
    public static Vector3 Subtract(Vector3 a, Vector3 b) => a - b;
    public static Vector3 Multiply(Vector3 a, float s) => a * s;

    public static Vector3 Cross(Vector3 a, Vector3 b) =>
        new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    public float Length() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

    public Vector3 Normalized()
    {
        var len = Length();
        return len > 0 ? new Vector3(X / len, Y / len, Z / len) : new Vector3(0, 1, 0);
    }
}

