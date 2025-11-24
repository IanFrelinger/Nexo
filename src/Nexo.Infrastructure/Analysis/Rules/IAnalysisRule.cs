using Nexo.Core.Application.Analysis.Models;

namespace Nexo.Infrastructure.Analysis.Rules;

/// <summary>
/// Interface for analysis rules following Strategy pattern (OCP).
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

