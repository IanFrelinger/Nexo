using Ashlar.Core.Application.Adaptation.Ports;

namespace Ashlar.Infrastructure.Adaptation;

/// <summary>
/// Explicit list of immutable core components. Documents what "immutable core" means at runtime.
/// Paths or namespaces matching these patterns must not be modified by self-adaptation.
/// </summary>
public sealed class ImmutableCoreRegistry : IImmutableCoreRegistry
{
    private static readonly string[] CorePathPatterns =
    {
        "Ashlar.Infrastructure/Observation",
        "Ashlar.Infrastructure/Analysis/BrickAnalyzer",
        "Ashlar.Infrastructure/Analysis/Rules",
        "Ashlar.Infrastructure/Validation",
        "Ashlar.Infrastructure/Adaptation/AdaptationPromoter",
        "Ashlar.Infrastructure/Trust",
        "Ashlar.Infrastructure/Rollback",
        "Ashlar.BackgroundAgents/Observation",
        "Ashlar.Core.Application/Validation",
        "Ashlar.Core.Application/Analysis/UseCases",
    };

    private static readonly string[] CoreIds =
    {
        "observation.pipeline",
        "analysis.engine",
        "validation.checker",
        "inheritance.protocol",
        "scope.boundary.enforcer",
        "dependency.graph",
        "rollback.manager",
    };

    /// <summary>Core component ids.</summary>
    public IReadOnlyList<string> CoreComponentIds { get; } = Array.AsReadOnly(CoreIds);

    /// <summary>Whether in immutable core.</summary>
    public bool IsInImmutableCore(string pathOrComponentId)
    {
        if (string.IsNullOrWhiteSpace(pathOrComponentId)) return false;

        var normalized = pathOrComponentId.Replace('\\', '/').Trim();
        if (CoreIds.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            return true;

        return IsCoreNamespace(normalized);
    }

    /// <summary>Whether core namespace.</summary>
    public bool IsCoreNamespace(string namespaceOrPath)
    {
        if (string.IsNullOrWhiteSpace(namespaceOrPath)) return false;

        var normalized = namespaceOrPath.Replace('\\', '/');
        foreach (var pattern in CorePathPatterns)
        {
            if (normalized.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
