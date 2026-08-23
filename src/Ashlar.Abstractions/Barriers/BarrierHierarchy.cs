using System.Collections;

namespace Ashlar.Abstractions.Barriers;

/// <summary>
/// Validated, ordered barrier hierarchy defined by the operator.
/// </summary>
public sealed class BarrierHierarchy : IEnumerable<string>
{
    private readonly List<BarrierLevel> _levels;
    private readonly Dictionary<string, BarrierLevel> _byName;

    /// <summary>
    /// Builds a validated hierarchy from operator-defined barrier levels.
    /// Levels are ordered by ascending rank; duplicate names or ranks are rejected.
    /// </summary>
    /// <param name="levels">Barrier levels defining the sensitivity ordering.</param>
    /// <exception cref="ArgumentException">Thrown when levels are empty, duplicated, or invalid.</exception>
    public BarrierHierarchy(IEnumerable<BarrierLevel> levels)
    {
        ThrowIfNullCompat(levels, nameof(levels));

        var ordered = levels
            .OrderBy(x => x.Rank)
            .ToList();
        if (ordered.Count == 0)
            throw new ArgumentException("At least one barrier level must be defined.", nameof(levels));

        var duplicateName = ordered
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateName != null)
            throw new ArgumentException($"Duplicate barrier level name '{duplicateName.Key}'.", nameof(levels));

        var duplicateRank = ordered
            .GroupBy(x => x.Rank)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateRank != null)
            throw new ArgumentException($"Duplicate barrier rank '{duplicateRank.Key}'.", nameof(levels));

        _levels = ordered;
        _byName = ordered.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Lowest-sensitivity barrier level in the hierarchy (floor).</summary>
    public BarrierLevel Floor => _levels[0];

    /// <summary>Highest-sensitivity barrier level in the hierarchy (ceiling).</summary>
    public BarrierLevel Ceiling => _levels[_levels.Count - 1];

    /// <summary>Returns whether <paramref name="name"/> matches a configured barrier level.</summary>
    /// <param name="name">Barrier level name to check.</param>
    public bool IsKnown(string name)
        => !string.IsNullOrWhiteSpace(name) && _byName.ContainsKey(name);

    /// <summary>
    /// Resolves a barrier level by name.
    /// </summary>
    /// <param name="name">Barrier level name.</param>
    /// <returns>The matching <see cref="BarrierLevel"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is unknown or empty.</exception>
    public BarrierLevel Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Barrier level name is required.", nameof(name));
        if (_byName.TryGetValue(name, out var level))
            return level;

        throw new ArgumentException($"Unknown barrier level '{name}'.", nameof(name));
    }

    /// <summary>
    /// Returns whether <paramref name="level"/> is at or below <paramref name="ceiling"/> sensitivity.
    /// </summary>
    public bool IsAtOrBelow(string level, string ceiling)
        => Get(level).Rank <= Get(ceiling).Rank;

    /// <summary>
    /// Returns whether <paramref name="level"/> is strictly above <paramref name="baseline"/> sensitivity.
    /// </summary>
    public bool IsAbove(string level, string baseline)
        => Get(level).Rank > Get(baseline).Rank;

    /// <summary>
    /// Returns the more sensitive of two barrier levels by rank.
    /// </summary>
    public BarrierLevel Highest(string a, string b)
    {
        var left = Get(a);
        var right = Get(b);
        return left.Rank >= right.Rank ? left : right;
    }

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator()
        => _levels.Select(x => x.Name).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    private static void ThrowIfNullCompat(object? value, string paramName)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null)
            throw new ArgumentNullException(paramName);
#endif
    }
}
