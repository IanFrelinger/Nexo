using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models.CodeQuality;

namespace Nexo.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for analyzing code quality
    /// </summary>
    public interface ICodeQualityAnalyzer
    {
        /// <summary>
        /// Analyzes the quality of generated code
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="toolName">Name of the tool being analyzed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comprehensive quality score and analysis</returns>
        Task<QualityScore> AnalyzeCodeAsync(string code, string toolName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes code for security vulnerabilities
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Security-specific quality score</returns>
        Task<QualityScore> AnalyzeSecurityAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes code for performance issues
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance-specific quality score</returns>
        Task<QualityScore> AnalyzePerformanceAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes code for maintainability issues
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maintainability-specific quality score</returns>
        Task<QualityScore> AnalyzeMaintainabilityAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes code for testability issues
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Testability-specific quality score</returns>
        Task<QualityScore> AnalyzeTestabilityAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines if code passes quality gates
        /// </summary>
        /// <param name="qualityScore">The quality score to evaluate</param>
        /// <returns>True if code passes quality gates</returns>
        bool PassesQualityGate(QualityScore qualityScore);

        /// <summary>
        /// Determines if code requires human review
        /// </summary>
        /// <param name="qualityScore">The quality score to evaluate</param>
        /// <returns>True if code requires human review</returns>
        bool RequiresHumanReview(QualityScore qualityScore);

        /// <summary>
        /// Determines if code is production ready
        /// </summary>
        /// <param name="qualityScore">The quality score to evaluate</param>
        /// <returns>True if code is production ready</returns>
        bool IsProductionReady(QualityScore qualityScore);
    }
}
