using Nexo.Spike.S0.Models;
using Nexo.Spike.S0.Roles;

namespace Nexo.Spike.S2.Adversary;

/// <summary>
/// Enforces that the adaptive adversary operates as implementer-only and never receives the reference oracle.
/// </summary>
public static class AdversaryAccessPolicy
{
    public static readonly string ReferenceOracleRelativePath = Path.Combine("artifacts", "s2", "reference-oracle.json");

    public static void EnsureImplementerOnly(AdaptiveAttemptContext context)
    {
        if (!context.ImplementerRole.CanEditImplementation)
            throw new InvalidOperationException("Adversary must hold implementer role with implementation edit rights.");

        if (context.ImplementerRole.CanEditTests)
            throw new InvalidOperationException("Adversary must not edit tests (RoleScope implementer).");

        if (ContainsOracleLeak(context))
        {
            throw new InvalidOperationException(
                "Reference oracle must not be visible to the adversary context.");
        }
    }

    public static bool ContainsOracleLeak(AdaptiveAttemptContext context)
    {
        if (context.VisibleSpec.IntentId.Contains("reference-oracle", StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var criterion in context.VisibleSpec.AcceptanceCriteria)
        {
            if (criterion.Contains("reference-oracle", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var verdict in context.PriorVerdicts)
        {
            if (verdict.RejectionDetail?.Contains("reference-oracle", StringComparison.OrdinalIgnoreCase) == true)
                return true;

            foreach (var disagreement in verdict.OracleDisagreements ?? [])
            {
                if (disagreement.ExpectedType.Contains("reference-oracle", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    public static RoleView CreateImplementerView(BrickSpec spec) => RoleScope.ForImplementer(spec);

    public static void AssertAdversaryCannotReadOracle()
    {
        var oraclePath = ResolveReferenceOraclePath();
        if (!File.Exists(oraclePath))
            throw new FileNotFoundException("Reference oracle fixture missing for isolation check.", oraclePath);

        var adversaryVisiblePaths = new[]
        {
            Path.Combine("src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json"),
            Path.Combine("samples", "spike-s0", "intents", "honest-csv-inferrer.json")
        };

        foreach (var visible in adversaryVisiblePaths)
        {
            var full = FindRepoFile(visible);
            if (full is not null &&
                Path.GetFullPath(full).Equals(Path.GetFullPath(oraclePath), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Adversary-visible intent path must not alias the reference oracle.");
            }
        }
    }

    internal static string ResolveReferenceOraclePath() => FindRepoFile(ReferenceOracleRelativePath)
        ?? throw new FileNotFoundException($"Reference oracle not found at {ReferenceOracleRelativePath}");

    internal static string? FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }
}
