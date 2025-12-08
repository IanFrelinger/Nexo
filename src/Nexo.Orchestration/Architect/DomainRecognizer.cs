using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Nexo.Orchestration.Architect;

/// <summary>
/// Recognizes domains from request text using pattern matching.
/// </summary>
public sealed class DomainRecognizer
{
    private readonly ILogger<DomainRecognizer> _logger;
    private readonly Dictionary<string, List<Regex>> _domainPatterns;

    public DomainRecognizer(ILogger<DomainRecognizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _domainPatterns = InitializeDomainPatterns();
    }

    /// <summary>
    /// Recognizes domains from a request text.
    /// </summary>
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
    /// </summary>
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

    private static Dictionary<string, List<Regex>> InitializeDomainPatterns()
    {
        return new Dictionary<string, List<Regex>>(StringComparer.OrdinalIgnoreCase)
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
            ["AI"] = new List<Regex>
            {
                new(@"\b(ai|artificial intelligence|agent|behavior|decision|pathfinding|navigation|steering)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(npc|non-player|character|bot|automated|intelligent|learning|neural|network)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
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
            },
            ["Gameplay"] = new List<Regex>
            {
                new(@"\b(gameplay|game|player|level|quest|mission|objective|achievement|progress|save|load)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(multiplayer|singleplayer|coop|pvp|pve|matchmaking|lobby|session)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            }
        };
    }
}

