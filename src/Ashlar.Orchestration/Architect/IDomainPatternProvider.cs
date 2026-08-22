using System.Text.RegularExpressions;

namespace Ashlar.Orchestration.Architect;

/// <summary>
/// Contributes domain-recognition patterns to <see cref="DomainRecognizer"/>.
///
/// <para>The kernel recognises only the domains it owns — Infrastructure, Security and the
/// general-purpose half of AI. Everything else is supplied by whoever installs it. Before
/// this seam existed, <c>DomainRecognizer</c> hardcoded regex tables for Combat, Economy and
/// Gameplay too, which meant the kernel could not describe its own vocabulary without also
/// describing a game's.</para>
///
/// <para>Patterns are MERGED per domain, not replaced. Two providers — or a provider and the
/// kernel — may both contribute to the same domain key, and every contributed pattern is
/// tried. That is what lets the game package extend "AI" with npc/pathfinding/steering while
/// the kernel keeps agent/neural/learning, rather than one side having to own the whole
/// domain. Keys are compared case-insensitively.</para>
/// </summary>
public interface IDomainPatternProvider
{
    /// <summary>
    /// Domain name to the patterns that recognise it. A request matching ANY pattern for a
    /// domain is treated as belonging to that domain.
    /// </summary>
    IReadOnlyDictionary<string, IReadOnlyList<Regex>> Patterns { get; }
}
