using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;

namespace Nexo.Runtime.Barriers.Identity.Resolvers;

internal sealed class PkiCertificateBarrierResolver : IBarrierIdentityResolver
{
    private const string SubjectField = "Subject";
    private const string SanField = "SAN";

    private readonly PkiCertificateResolverOptions _options;
    private readonly BarrierHierarchy _hierarchy;
    private readonly ILogger<PkiCertificateBarrierResolver> _logger;

    public PkiCertificateBarrierResolver(
        PkiCertificateResolverOptions options,
        BarrierHierarchy hierarchy,
        ILogger<PkiCertificateBarrierResolver> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ResolverName => "PkiCertificate";

    public ValueTask<BarrierResolutionResult?> TryResolveAsync(
        BarrierResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (context is null)
            {
                return default;
            }

            foreach (var rule in _options.Rules)
            {
                if (rule is null)
                    continue;

                var level = (rule.BarrierLevel ?? string.Empty).Trim();
                if (!_hierarchy.IsKnown(level))
                {
                    _logger.LogWarning(
                        "Skipping PKI resolver rule with unknown barrier level. Rule={RuleName} BarrierLevel={BarrierLevel}",
                        rule.Name,
                        rule.BarrierLevel);
                    continue;
                }

                var candidates = ResolveCandidates(rule.MatchField, context);
                if (candidates.Count == 0)
                    continue;

                var pattern = rule.MatchPattern ?? string.Empty;
                var matched = candidates.FirstOrDefault(candidate => IsMatch(candidate, pattern));
                if (string.IsNullOrWhiteSpace(matched))
                    continue;

                var detail = string.Equals(rule.MatchField, SubjectField, StringComparison.OrdinalIgnoreCase)
                    ? $"Subject '{matched}' matched rule '{rule.Name}'."
                    : $"SAN '{matched}' matched rule '{rule.Name}'.";

                var result = new BarrierResolutionResult(
                    ResolvedLevel: level,
                    ResolverName: ResolverName,
                    AuthoritySource: BarrierAuthoritySource.PkiCertificate,
                    Detail: detail);

                return new ValueTask<BarrierResolutionResult?>(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PKI barrier resolution failed.");
        }

        return default;
    }

    private static IReadOnlyList<string> ResolveCandidates(string? matchField, BarrierResolutionContext context)
    {
        if (string.Equals(matchField, SubjectField, StringComparison.OrdinalIgnoreCase))
            return context.CertSubjects;
        if (string.Equals(matchField, SanField, StringComparison.OrdinalIgnoreCase))
            return context.CertSans;
        return Array.Empty<string>();
    }

    private static bool IsMatch(string candidate, string pattern)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(pattern))
            return false;

        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return string.Equals(candidate, pattern, StringComparison.OrdinalIgnoreCase);

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(candidate, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
