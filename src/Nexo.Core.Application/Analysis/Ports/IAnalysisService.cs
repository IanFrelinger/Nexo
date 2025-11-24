using Nexo.Core.Application.Analysis.Models;

namespace Nexo.Core.Application.Analysis.Ports;

/// <summary>
/// Port for code/assembly analysis operations.
/// </summary>
public interface IAnalysisService
{
    /// <summary>
    /// Analyzes code and assemblies at the specified path for violations and issues.
    /// </summary>
    Task<AnalysisResult> AnalyzeAsync(
        DirectoryInfo path,
        CancellationToken cancellationToken = default);
}

