namespace Ashlar.Commercial.GameDomain.Discord;
/// <summary>
/// Maps Discord emoji reactions to fix application actions.
/// ✅ = approve all fixes, ❌ = reject all, 1️⃣-9️⃣ = approve individual fix.
/// Logs all decisions to the adaptation log for traceability.
/// </summary>
public sealed class ApprovalBridge
{
    private static readonly Dictionary<string, int> NumberEmojiMap = new()
    {
        ["1️⃣"] = 1, ["2️⃣"] = 2, ["3️⃣"] = 3, ["4️⃣"] = 4, ["5️⃣"] = 5,
        ["6️⃣"] = 6, ["7️⃣"] = 7, ["8️⃣"] = 8, ["9️⃣"] = 9
    };

    /// <summary>
    /// Resolve which fixes to apply based on the emoji reaction.
    /// </summary>
    public static ApprovalResult Resolve(string emoji, IReadOnlyList<Playtest.ProposedFix> fixes)
    {
        if (emoji == "✅")
            return new ApprovalResult(
                ApprovalAction.ApproveAll,
                fixes.ToList(),
                $"All {fixes.Count} fix(es) approved");

        if (emoji == "❌")
            return new ApprovalResult(
                ApprovalAction.RejectAll,
                Array.Empty<Playtest.ProposedFix>(),
                "All fixes rejected");

        if (NumberEmojiMap.TryGetValue(emoji, out var fixNumber))
        {
            var fix = fixes.FirstOrDefault(f => f.FixNumber == fixNumber);
            if (fix != null)
                return new ApprovalResult(
                    ApprovalAction.ApproveSingle,
                    new[] { fix },
                    $"Fix #{fixNumber} approved: {fix.Description}");
        }

        return new ApprovalResult(
            ApprovalAction.Unknown,
            Array.Empty<Playtest.ProposedFix>(),
            $"Unrecognized reaction: {emoji}");
    }
}
