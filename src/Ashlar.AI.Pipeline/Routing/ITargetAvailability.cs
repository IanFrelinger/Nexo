namespace Ashlar.AI.Pipeline.Routing;

/// <summary>
/// Reports whether an execution target is currently healthy/available.
/// </summary>
public interface ITargetAvailability
{
    /// <summary>Returns true when the target can accept work right now.</summary>
    bool IsAvailable(string targetKey);
}

/// <summary>
/// Mutable in-memory availability map (tests + simple hosts).
/// </summary>
public sealed class InMemoryTargetAvailability : ITargetAvailability
{
    private readonly Dictionary<string, bool> _map =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Sets availability for a target key.</summary>
    public InMemoryTargetAvailability Set(string targetKey, bool available)
    {
        _map[targetKey] = available;
        return this;
    }

    /// <inheritdoc />
    public bool IsAvailable(string targetKey) =>
        !_map.TryGetValue(targetKey, out var available) || available;
}
