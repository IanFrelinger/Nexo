using Ashlar.Core.Application.Analysis.Models;

namespace Ashlar.Infrastructure.Analysis.Rules;

/// <summary>
/// Interface for analysis rules following Strategy pattern (OCP).
/// 
/// Defines the contract for analysis rules that can be applied to assemblies.
/// Each rule analyzes an assembly file and returns a list of violations.
/// 
/// Implementations (e.g., SecurityAnalysisRule, CodeQualityRule) provide
/// specific analysis logic. Rules are registered with AnalysisRuleEngine.
/// 
/// Follows Open/Closed Principle - new rules can be added without modifying existing code.
/// </summary>
public interface IAnalysisRule
{
    /// <summary>
    /// Gets the name of the rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the rule.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Analyzes an assembly file and returns violations.
    /// </summary>
    Task<IReadOnlyList<Violation>> AnalyzeAsync(
        FileInfo assemblyFile,
        CancellationToken cancellationToken = default);
}

