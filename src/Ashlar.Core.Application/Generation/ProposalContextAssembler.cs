using System.Security.Cryptography;
using System.Text;

namespace Ashlar.Core.Application.Generation;

/// <summary>
/// Deterministic stage-1 context assembly (trust-loop spec §1, R1.2/R1.4/R1.5): the
/// "aim" step that turns candidate sources into a budgeted, canonicalized, hashable
/// <see cref="ProposalContext"/> before any model is invoked.
/// </summary>
/// <remarks>
/// Determinism contract: element order is (tier, kind, id, content) with ordinal
/// comparisons — independent of input order; content is canonicalized to LF line
/// endings before measuring, truncating, or hashing; every source class is explicitly
/// budgeted (a declared per-class cap, else the default cap — there is no unbudgeted
/// class); what was truncated or omitted is recorded, never silently dropped; and any
/// seed for randomized steps is part of the hashed canonical form (R1.2).
/// </remarks>
public sealed class ProposalContextAssembler
{
    private readonly int _defaultMaxCharsPerKind;
    private readonly Dictionary<string, int> _budgets;
    private readonly string? _seed;

    /// <summary>Initializes the assembler.</summary>
    /// <param name="defaultMaxCharsPerKind">Cap for source classes without an explicit budget.</param>
    /// <param name="budgets">Explicit per-class caps overriding the default.</param>
    /// <param name="seed">Recorded seed when any preparation step is randomized (spec R1.2).</param>
    public ProposalContextAssembler(
        int defaultMaxCharsPerKind,
        IReadOnlyList<ProposalContextBudget>? budgets = null,
        string? seed = null)
    {
        if (defaultMaxCharsPerKind <= 0)
            throw new ArgumentOutOfRangeException(nameof(defaultMaxCharsPerKind), "The default per-class budget must be positive.");

        _defaultMaxCharsPerKind = defaultMaxCharsPerKind;
        _budgets = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var budget in budgets ?? Array.Empty<ProposalContextBudget>())
        {
            if (budget.MaxChars <= 0)
                throw new ArgumentOutOfRangeException(nameof(budgets), $"Budget for '{budget.Kind}' must be positive.");
            _budgets[budget.Kind] = budget.MaxChars;
        }

        _seed = seed;
    }

    /// <summary>Assembles the canonical context from candidate sources.</summary>
    public ProposalContext Assemble(IReadOnlyList<ProposalContextSource> sources)
    {
        if (sources is null)
            throw new ArgumentNullException(nameof(sources));

        var ordered = sources
            .Select(s => new
            {
                s.Kind,
                s.Id,
                s.Tier,
                Content = Canonicalize(s.Content)
            })
            .OrderBy(s => (int)s.Tier)
            .ThenBy(s => s.Kind, StringComparer.Ordinal)
            .ThenBy(s => s.Id, StringComparer.Ordinal)
            .ThenBy(s => s.Content, StringComparer.Ordinal)
            .ToArray();

        var spentByKind = new Dictionary<string, int>(StringComparer.Ordinal);
        var elements = new List<ProposalContextElement>();
        var omitted = new List<string>();

        foreach (var source in ordered)
        {
            var cap = _budgets.TryGetValue(source.Kind, out var explicitCap)
                ? explicitCap
                : _defaultMaxCharsPerKind;
            spentByKind.TryGetValue(source.Kind, out var spent);
            var remaining = cap - spent;

            if (remaining <= 0)
            {
                omitted.Add($"{source.Kind}/{source.Id}");
                continue;
            }

            var truncated = source.Content.Length > remaining;
            var included = truncated ? source.Content.Substring(0, remaining) : source.Content;
            spentByKind[source.Kind] = spent + included.Length;

            elements.Add(new ProposalContextElement
            {
                Kind = source.Kind,
                Id = source.Id,
                Tier = source.Tier,
                Content = included,
                Truncated = truncated,
                OriginalLength = source.Content.Length,
                ContentHash = HashText(included)
            });
        }

        return new ProposalContext
        {
            Hash = HashText(BuildCanonicalForm(elements, omitted)),
            Elements = elements,
            OmittedSourceIds = omitted,
            Seed = _seed
        };
    }

    private string BuildCanonicalForm(
        IReadOnlyList<ProposalContextElement> elements,
        IReadOnlyList<string> omitted)
    {
        var builder = new StringBuilder();
        builder.Append("seed=").Append(_seed ?? "").Append('\n');
        foreach (var element in elements)
        {
            builder.Append("kind=").Append(element.Kind)
                .Append(";id=").Append(element.Id)
                .Append(";tier=").Append((int)element.Tier)
                .Append(";truncated=").Append(element.Truncated ? "1" : "0")
                .Append(";hash=").Append(element.ContentHash)
                .Append('\n');
        }

        builder.Append("omitted=").Append(string.Join(",", omitted));
        return builder.ToString();
    }

    /// <summary>LF-normalizes line endings so hashes are stable across platforms.</summary>
    private static string Canonicalize(string content) =>
        (content ?? "").Replace("\r\n", "\n").Replace("\r", "\n");

    // SHA256.Create + ToBase64String: HashData/ToHexString are unavailable on the
    // netstandard2.0 leg of this project. Base64 SHA-256 matches the certification
    // subsystem's content-hash encoding, so this hash can feed certificate inputs.
    private static string HashText(string text)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
    }
}
