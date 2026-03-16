using System.Collections;

namespace Nexo.Abstractions.Barriers;

/// <summary>
/// Validated, ordered barrier hierarchy defined by the operator.
/// </summary>
public sealed class BarrierHierarchy : IEnumerable<string>
{
    private readonly List<BarrierLevel> _levels;
    private readonly Dictionary<string, BarrierLevel> _byName;

    public BarrierHierarchy(IEnumerable<BarrierLevel> levels)
    {
        ArgumentNullException.ThrowIfNull(levels);

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

    public BarrierLevel Floor => _levels[0];

    public BarrierLevel Ceiling => _levels[^1];

    public bool IsKnown(string name)
        => !string.IsNullOrWhiteSpace(name) && _byName.ContainsKey(name);

    public BarrierLevel Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Barrier level name is required.", nameof(name));
        if (_byName.TryGetValue(name, out var level))
            return level;

        throw new ArgumentException($"Unknown barrier level '{name}'.", nameof(name));
    }

    public bool IsAtOrBelow(string level, string ceiling)
        => Get(level).Rank <= Get(ceiling).Rank;

    public bool IsAbove(string level, string baseline)
        => Get(level).Rank > Get(baseline).Rank;

    public BarrierLevel Highest(string a, string b)
    {
        var left = Get(a);
        var right = Get(b);
        return left.Rank >= right.Rank ? left : right;
    }

    public IEnumerator<string> GetEnumerator()
        => _levels.Select(x => x.Name).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
