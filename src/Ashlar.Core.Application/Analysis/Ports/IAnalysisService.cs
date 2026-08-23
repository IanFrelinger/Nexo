using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Common.Models;

namespace Ashlar.Core.Application.Analysis.Ports;

/// <summary>
/// Port for code/assembly analysis operations.
/// </summary>
public interface IAnalysisService
{
    /// <summary>
    /// Analyzes code and assemblies at the specified path for violations and issues.
    /// </summary>
    /// <param name="path">Directory path to analyze.</param>
    /// <param name="progress">Optional progress reporter for streaming updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AnalysisResult> AnalyzeAsync(
        DirectoryInfo path,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}

