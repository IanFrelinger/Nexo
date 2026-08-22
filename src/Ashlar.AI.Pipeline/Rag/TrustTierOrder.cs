namespace Ashlar.AI.Pipeline.Rag;

/// <summary>
/// Orders trust/sensitivity tier names for RAG filtering (lower = less sensitive).
/// Mirrors Ashlar data-sensitivity primitives without referencing BackgroundAgents.
/// </summary>
public static class TrustTierOrder
{
    private static readonly Dictionary<string, int> Ranks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Public"] = 0,
        ["Internal"] = 1,
        ["Confidential"] = 2,
        ["Secret"] = 3,
        ["TopSecret"] = 4,
    };

    /// <summary>Returns the numeric rank for a tier name (unknown ⇒ TopSecret for fail-closed).</summary>
    public static int Rank(string? tierName)
    {
        if (string.IsNullOrWhiteSpace(tierName))
        {
            return Ranks["Public"];
        }

        return Ranks.TryGetValue(tierName.Trim(), out var rank) ? rank : Ranks["TopSecret"];
    }

    /// <summary>True when record tier is at or below the caller's max tier.</summary>
    public static bool IsAllowed(string? recordTier, string? callerMaxTier) =>
        Rank(recordTier) <= Rank(callerMaxTier);
}
