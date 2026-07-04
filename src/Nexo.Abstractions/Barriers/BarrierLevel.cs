namespace Nexo.Abstractions.Barriers;

/// <summary>
/// Operator-defined barrier level with ordered rank.
/// </summary>
public sealed record BarrierLevel : IComparable<BarrierLevel>
{
    /// <summary>
    /// Creates a barrier level with a human-readable name and sensitivity rank.
    /// </summary>
    /// <param name="name">Operator-defined level label.</param>
    /// <param name="rank">Sensitivity rank; lower values indicate lower sensitivity.</param>
    public BarrierLevel(string name, int rank)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Barrier level name is required.", nameof(name));

        Name = name;
        Rank = rank;
    }

    /// <summary>Human-readable, operator-defined label.</summary>
    public string Name { get; }

    /// <summary>Sensitivity rank. Lower rank means lower sensitivity.</summary>
    public int Rank { get; }

    /// <inheritdoc />
    public int CompareTo(BarrierLevel? other)
        => other is null ? 1 : Rank.CompareTo(other.Rank);

    /// <inheritdoc />
    public bool Equals(BarrierLevel? other)
        => other is not null && Rank == other.Rank;

    /// <inheritdoc />
    public override int GetHashCode()
        => Rank.GetHashCode();

    public static bool operator <(BarrierLevel? left, BarrierLevel? right)
        => Comparer<BarrierLevel?>.Default.Compare(left, right) < 0;

    public static bool operator <=(BarrierLevel? left, BarrierLevel? right)
        => Comparer<BarrierLevel?>.Default.Compare(left, right) <= 0;

    public static bool operator >(BarrierLevel? left, BarrierLevel? right)
        => Comparer<BarrierLevel?>.Default.Compare(left, right) > 0;

    public static bool operator >=(BarrierLevel? left, BarrierLevel? right)
        => Comparer<BarrierLevel?>.Default.Compare(left, right) >= 0;
}
