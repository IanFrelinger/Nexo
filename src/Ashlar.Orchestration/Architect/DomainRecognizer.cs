using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Ashlar.Orchestration.Architect;

/// <summary>
/// Recognizes domains from request text using pattern matching.
/// 
/// Responsibilities:
/// - Identifies domains from request text, using the kernel's own patterns plus any
///   contributed by registered IDomainPatternProvider implementations
/// - Extracts keywords for semantic matching
/// - Uses regex patterns to match domain-specific terminology
/// 
/// Used by ArchitectAgent and DecompositionRetriever to understand request context.
/// </summary>
public sealed class DomainRecognizer
{
    private readonly ILogger<DomainRecognizer> _logger;
    private readonly Dictionary<string, List<Regex>> _domainPatterns;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainRecognizer"/> class.
    ///
    /// The kernel contributes Infrastructure, Security and general-purpose AI. Any further
    /// domains — and any additional patterns for the kernel's own domains — come from the
    /// registered <see cref="IDomainPatternProvider"/>s.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="patternProviders">
    /// Providers contributing further domain patterns. Optional so existing
    /// <c>new DomainRecognizer(logger)</c> call sites keep compiling; DI resolves it to an
    /// empty sequence when nothing is registered.
    /// </param>
    public DomainRecognizer(
        ILogger<DomainRecognizer> logger,
        IEnumerable<IDomainPatternProvider>? patternProviders = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _domainPatterns = InitializeDomainPatterns();

        foreach (var provider in patternProviders ?? Enumerable.Empty<IDomainPatternProvider>())
        {
            foreach (var (domain, patterns) in provider.Patterns)
            {
                // Merge, do not replace: a provider extending a domain the kernel already
                // knows about (the game half of "AI", say) must add to it rather than
                // silently discard the kernel's own patterns for that domain.
                if (_domainPatterns.TryGetValue(domain, out var existing))
                {
                    existing.AddRange(patterns);
                }
                else
                {
                    _domainPatterns[domain] = patterns.ToList();
                }
            }
        }
    }

    /// <summary>
    /// Recognizes domains from a request text using pattern matching.
    /// 
    /// Uses regex patterns to match domain-specific terminology in the request.
    /// Returns all domains that match (a request can belong to multiple domains).
    /// </summary>
    /// <param name="request">The request text to analyze.</param>
    /// <returns>A read-only list of recognized domain names (e.g., "Combat", "Economy", "AI").</returns>
    public IReadOnlyList<string> RecognizeDomains(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            return Array.Empty<string>();
        }

        var recognizedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalizedRequest = request.ToLowerInvariant();

        foreach (var (domain, patterns) in _domainPatterns)
        {
            foreach (var pattern in patterns)
            {
                if (pattern.IsMatch(normalizedRequest))
                {
                    recognizedDomains.Add(domain);
                    _logger.LogDebug("Recognized domain '{Domain}' from request", domain);
                    break;
                }
            }
        }

        return recognizedDomains.ToList();
    }

    /// <summary>
    /// Extracts keywords from a request for semantic matching.
    /// 
    /// Process:
    /// - Splits text by whitespace and punctuation
    /// - Filters out stop words and short words (length <= 3)
    /// - Returns up to 20 unique keywords (normalized to lowercase)
    /// 
    /// Used for similarity matching in RAG retrieval.
    /// </summary>
    /// <param name="request">The request text to extract keywords from.</param>
    /// <returns>A read-only list of extracted keywords (up to 20).</returns>
    public IReadOnlyList<string> ExtractKeywords(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            return Array.Empty<string>();
        }

        // Simple keyword extraction: split by common delimiters and filter
        var words = Regex.Split(request, @"[\s\p{P}]+")
            .Where(w => w.Length > 3) // Filter short words
            .Where(w => !IsStopWord(w))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .Take(20) // Limit to top 20 keywords
            .ToList();

        return words;
    }

    /// <summary>
    /// Determines if a word is a stop word (common words that don't carry semantic meaning).
    /// 
    /// Stop words include: articles (the, a, an), prepositions (in, on, at), conjunctions (and, or, but),
    /// common verbs (is, was, are), and other high-frequency words.
    /// </summary>
    /// <param name="word">The word to check.</param>
    /// <returns>True if the word is a stop word, false otherwise.</returns>
    private static bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "as", "is", "was", "are", "were", "be",
            "been", "being", "have", "has", "had", "do", "does", "did", "will",
            "would", "should", "could", "may", "might", "must", "can", "this",
            "that", "these", "those", "what", "which", "who", "when", "where",
            "why", "how", "all", "each", "every", "some", "any", "no", "not"
        };

        return stopWords.Contains(word);
    }

    /// <summary>
    /// The domains the KERNEL itself recognises: Infrastructure, Security, and the
    /// general-purpose half of AI.
    ///
    /// Combat, Economy and Gameplay used to live here too, along with the game half of AI
    /// (pathfinding, npc, steering). They are game vocabulary, and a kernel that has no game
    /// layer installed has no business recognising them, so they moved to
    /// GameDomainPatternProvider. The 17 AI terms are split 11 here / 6 there and re-merged
    /// by the constructor, so nothing is lost when both are present.
    /// </summary>
    /// <returns>A dictionary mapping domain names to their regex patterns.</returns>
    private static Dictionary<string, List<Regex>> InitializeDomainPatterns()
    {
        return new Dictionary<string, List<Regex>>(StringComparer.OrdinalIgnoreCase)
        {
            ["AI"] = new List<Regex>
            {
                new(@"\b(ai|artificial intelligence|agent|behavior|decision)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(bot|automated|intelligent|learning|neural|network)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            },
            ["Infrastructure"] = new List<Regex>
            {
                new(@"\b(infrastructure|server|network|database|storage|api|service|microservice|deployment)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(cloud|aws|azure|gcp|kubernetes|docker|container|scaling|load|balance)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            },
            ["Security"] = new List<Regex>
            {
                new(@"\b(security|auth|authentication|authorization|encryption|password|token|jwt|oauth)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(vulnerability|threat|attack|defense|firewall|ssl|tls|certificate|permission)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            }
        };
    }
}

