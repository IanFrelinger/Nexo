using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Analysis.Ports;

/// <summary>
/// Behavioral analyzer: compares actual brick output to declared output contract.
/// Flags drift when output diverges from expected.
/// </summary>
public interface IBehavioralAnalyzer
{
    /// <summary>
    /// Compares actual brick output to the declared BrickInterface output contract.
    /// </summary>
    /// <param name="brick">The brick that was executed.</param>
    /// <param name="actualOutput">The BrickOutput produced.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating contract satisfaction and any drift.</returns>
    Task<BehavioralAnalysisResult> AnalyzeAsync(
        DomainBrick brick,
        BrickOutput actualOutput,
        CancellationToken cancellationToken = default);
}
