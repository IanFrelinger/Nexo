using System.Diagnostics.CodeAnalysis;

namespace Nexo.Core.Application.Autonomy;

/// <summary>
/// The trust kernel enumeration (autonomous self-extension spec I-1): gates, policies,
/// admission tiers, the swap host, the certifier, budgets, and this enforcement code
/// itself. No artifact produced by the loop may modify anything in this enumeration
/// without human admission — under any tier, at any depth, under any objective.
///
/// The enumeration is STATIC and versioned in source on purpose (I-2: autonomy is bounded
/// by a static classification, never by scores or history). Widening Tier 0 — i.e.
/// shrinking this list — is itself a Tier 2 change (R3.3), which is why this file's own
/// path is inside the enumeration it defines.
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public static class TrustKernel
{
    /// <summary>Repo-relative path prefixes of the trust kernel (forward slashes).</summary>
    public static readonly IReadOnlyList<string> KernelPathPrefixes = new[]
    {
        // Certification: gates, signer, verifier, mutation engine, swap host, analyzer fence.
        "src/Nexo.Infrastructure/Certification/",
        "src/Nexo.Core.Application/Certification/",
        // The analyzer catalog — the loop may not rewrite its own judges (R4.3).
        "src/Nexo.Analyzers/",
        // Policy surfaces: tool policy engine inputs, path allowlists, security policies.
        "src/Nexo.Policies/",
        "src/Nexo.Policies.Dev/",
        "src/Nexo.BackgroundAgents/Security/",
        // Sandbox/session contracts and confinement derivations.
        "src/Nexo.Core.Application/Execution/",
        "src/Nexo.Infrastructure/Execution/Sandbox/",
        // This spec's enforcement code: tiers, touch-sets, budgets, the kernel list itself.
        "src/Nexo.Core.Application/Autonomy/",
        // CI enforcement is part of the kernel: gates that run in CI are still gates.
        "scripts/",
        ".github/",
    };

    /// <summary>Namespace prefixes of the trust kernel.</summary>
    public static readonly IReadOnlyList<string> KernelNamespacePrefixes = new[]
    {
        "Nexo.Infrastructure.Certification",
        "Nexo.Core.Application.Certification",
        "Nexo.Analyzers",
        "Nexo.Policies",
        "Nexo.BackgroundAgents.Security",
        "Nexo.Core.Application.Execution",
        "Nexo.Infrastructure.Execution.Sandbox",
        "Nexo.Core.Application.Autonomy",
    };

    /// <summary>
    /// The authority-rule subset whose OBJECTIVES must originate from a human (Tier 2,
    /// R3.1): tier classification rules, budgets, and this spec's enforcement. Machine
    /// sources must not even open a proposal session against these.
    /// </summary>
    public static readonly IReadOnlyList<string> AuthorityRulePathPrefixes = new[]
    {
        "src/Nexo.Core.Application/Autonomy/",
    };

    /// <summary>Whether a repo-relative path lies inside the trust kernel.</summary>
    public static bool IsKernelPath(string path) => MatchesPrefix(path, KernelPathPrefixes);

    /// <summary>Whether a namespace lies inside the trust kernel.</summary>
    public static bool IsKernelNamespace(string ns)
    {
        var candidate = ns?.Trim() ?? "";
        foreach (var prefix in KernelNamespacePrefixes)
        {
            if (candidate.Equals(prefix, StringComparison.Ordinal)
                || candidate.StartsWith(prefix + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether a repo-relative path lies inside the authority-rule (Tier 2) subset.</summary>
    public static bool IsAuthorityRulePath(string path) => MatchesPrefix(path, AuthorityRulePathPrefixes);

    /// <summary>Every touch-set entry that reaches into the kernel, for refusal messages.</summary>
    public static IReadOnlyList<string> KernelIntersections(TouchSet touchSet)
    {
        var hits = new List<string>();
        foreach (var path in touchSet.PathPrefixes)
        {
            if (IsKernelPath(path))
                hits.Add($"path {path}");
        }

        foreach (var ns in touchSet.Namespaces)
        {
            if (IsKernelNamespace(ns))
                hits.Add($"namespace {ns}");
        }

        return hits;
    }

    private static bool MatchesPrefix(string path, IReadOnlyList<string> prefixes)
    {
        var normalized = (path ?? "").Replace('\\', '/').TrimStart('/');
        foreach (var prefix in prefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
