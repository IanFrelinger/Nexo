namespace Nexo.Abstractions.Barriers;

/// <summary>
/// Operator-defined barrier level with ordered rank.
/// </summary>
public sealed record BarrierLevel : IComparable<BarrierLevel>
{
    public BarrierLevel(string name, int rank)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Barrier level name is required.", nameof(name));

        Name = name;
        Rank = rank;
    }

    /// <summary>
    /// Human-readable, operator-defined label.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Sensitivity rank. Lower rank means lower sensitivity.
    /// </summary>
    public int Rank { get; }

    public int CompareTo(BarrierLevel? other)
        => other is null ? 1 : Rank.CompareTo(other.Rank);

    public bool Equals(BarrierLevel? other)
        => other is not null && Rank == other.Rank;

    public override int GetHashCode()
        => Rank.GetHashCode();

    public static bool operator <(BarrierLevel left, BarrierLevel right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(BarrierLevel left, BarrierLevel right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(BarrierLevel left, BarrierLevel right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(BarrierLevel left, BarrierLevel right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) >= 0;
    }
}
