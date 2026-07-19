namespace Nexo.GameDomain.Contracts;

/// <summary>
/// Stable simulation entity identifier. It is intentionally independent of Unity
/// instance IDs and networking-library object handles.
/// </summary>
public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
{
    /// <summary>Creates an entity identifier.</summary>
    public EntityId(ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Entity id zero is reserved.");

        Value = value;
    }

    /// <summary>Underlying stable value.</summary>
    public ulong Value { get; }

    /// <inheritdoc />
    public bool Equals(EntityId other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EntityId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Compares two entity identifiers.</summary>
    public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);

    /// <summary>Compares two entity identifiers.</summary>
    public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
}
