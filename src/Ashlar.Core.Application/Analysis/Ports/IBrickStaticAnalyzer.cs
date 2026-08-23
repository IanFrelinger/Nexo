using Ashlar.Core.Application.Analysis.Models;

namespace Ashlar.Core.Application.Analysis.Ports;

/// <summary>
/// Static analyzer for generated bricks and code. Validates schema, safety, and performance
/// before deployment. Dogfood: must analyze Block 1 (observation pipeline) code first.
/// </summary>
public interface IBrickStaticAnalyzer
{
    /// <summary>
    /// Analyzes C# source files at the given path for schema, safety, and performance issues.
    /// </summary>
    /// <param name="sourcePath">Directory containing .cs files, or a single .cs file.</param>
    /// <param name="includeAnalyzers">When true, also include Analysis and Adaptation folders (recursive self-analysis).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis result with violations (empty if passed).</returns>
    Task<BrickStaticAnalysisResult> AnalyzeSourceAsync(
        string sourcePath,
        bool includeAnalyzers = false,
        CancellationToken cancellationToken = default);
}
