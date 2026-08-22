using System.Text.RegularExpressions;
using Ashlar.Orchestration.Architect;

namespace Ashlar.Orchestration.GameDomain;

/// <summary>
/// Game-domain recognition patterns: Combat, Economy, Gameplay, and the game half of AI.
///
/// <para>These were hardcoded in <see cref="DomainRecognizer"/>, which meant the kernel could
/// not describe its own vocabulary without also describing a game's. They live here so they
/// leave with the game layer.</para>
///
/// <para>Every pattern is carried over verbatim from the table this replaced, including the
/// duplicated <c>economy|economy</c> alternation in the first Economy pattern. A duplicate
/// alternation is exactly equivalent to a single one, so it changes nothing — it is kept only
/// so that a diff of the regexes shows no edits at all.</para>
/// </summary>
public sealed class GameDomainPatternProvider : IDomainPatternProvider
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<Regex>> PatternTable =
        new Dictionary<string, IReadOnlyList<Regex>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Combat"] = new List<Regex>
            {
                new(@"\b(combat|battle|fight|weapon|damage|health|armor|attack|defense|enemy|player|kill|death)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(shoot|gun|rifle|pistol|sword|melee|ranged|ammo|reload)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            },
            ["Economy"] = new List<Regex>
            {
                new(@"\b(economy|economy|money|currency|price|cost|buy|sell|trade|market|shop|vendor|item|inventory)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(resource|gold|silver|coin|transaction|purchase|payment|reward|loot)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            },
            ["Gameplay"] = new List<Regex>
            {
                new(@"\b(gameplay|game|player|level|quest|mission|objective|achievement|progress|save|load)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(multiplayer|singleplayer|coop|pvp|pve|matchmaking|lobby|session)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            },

            // The game half of the original AI table. The kernel keeps the general-purpose
            // terms (ai, agent, behavior, decision, neural, learning, ...) because Ashlar is
            // an agent framework and those are its own vocabulary; these six are specifically
            // game AI. DomainRecognizer merges both lists under the same "AI" key, so with
            // the game layer installed all 17 original terms are recognised exactly as before.
            ["AI"] = new List<Regex>
            {
                new(@"\b(pathfinding|navigation|steering|npc|non-player|character)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            }
        };

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IReadOnlyList<Regex>> Patterns => PatternTable;
}
