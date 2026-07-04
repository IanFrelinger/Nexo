using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Explicit list of immutable core components. Documents what "immutable core" means at runtime.
/// Paths or namespaces matching these patterns must not be modified by self-adaptation.
/// </summary>
public sealed class ImmutableCoreRegistry : IImmutableCoreRegistry
{
    private static readonly string[] CorePathPatterns =
    {
        "Nexo.Infrastructure/Observation",
        "Nexo.Infrastructure/Analysis/BrickAnalyzer",
        "Nexo.Infrastructure/Analysis/Rules",
        "Nexo.Infrastructure/Validation",
        "Nexo.Infrastructure/Adaptation/AdaptationPromoter",
        "Nexo.Infrastructure/Trust",
        "Nexo.Infrastructure/Rollback",
        "Nexo.BackgroundAgents/Observation",
        "Nexo.Core.Application/Validation",
        "Nexo.Core.Application/Analysis/UseCases",
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
