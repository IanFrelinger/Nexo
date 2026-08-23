using System.Text.Json;

namespace Ashlar.Abstractions;

/// <summary>
/// Concrete implementation of IActionDelta representing a world state change.
///
/// Contains tick range, log messages, and optional cryptographic signature.
/// Provides a static Merge method to combine multiple deltas into one.
/// </summary>
/// <param name="TickFrom">Starting tick of this delta.</param>
/// <param name="TickTo">Ending tick of this delta.</param>
/// <param name="Log">Log messages describing the actions taken.</param>
public sealed record ActionDelta(int TickFrom, int TickTo, IReadOnlyList<string> Log) : IActionDelta
{
    /// <summary>
    /// Cryptographic signature (SHA256 hash) for integrity verification.
    /// </summary>
    public IReadOnlyList<byte>? Signature { get; set; }

    /// <summary>
    /// Merges multiple action deltas into a single delta.
    /// Combines tick ranges and concatenates log messages.
    /// </summary>
    /// <param name="deltas">The deltas to merge.</param>
    /// <returns>A merged action delta, or an empty delta if the input is empty.</returns>
    public static IActionDelta Merge(IEnumerable<IActionDelta> deltas)
    {
        var list = deltas.ToList();
        if (list.Count == 0) return new ActionDelta(0, 0, Array.Empty<string>());
        var from = list.Min(d => d.TickFrom);
        var to = list.Max(d => d.TickTo);
        var log = list.SelectMany(d => d.Log).ToList();
        return new ActionDelta(from, to, log);
    }
}
