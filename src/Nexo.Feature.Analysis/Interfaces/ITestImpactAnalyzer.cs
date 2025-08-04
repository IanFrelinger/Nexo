using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Interface for analyzing the impact of code changes on tests.
    /// </summary>
    public interface ITestImpactAnalyzer
    {
        /// <summary>
        /// Analyzes the impact of changed files on test selection.
        /// </summary>
        /// <param name="changedFiles">List of changed file paths.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Test impact analysis result.</returns>
        Task<TestImpactAnalysis> AnalyzeImpactAsync(List<string> changedFiles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tests that are likely affected by changes to specific files.
        /// </summary>
        /// <param name="changedFiles">List of changed file paths.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of test file paths that should be run.</returns>
        Task<List<string>> GetAffectedTestsAsync(List<string> changedFiles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Maps a source file to its related test files.
        /// </summary>
        /// <param name="sourceFile">Source file path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of related test file paths.</returns>
        Task<List<string>> MapSourceToTestsAsync(string sourceFile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the confidence level for test impact analysis.
        /// </summary>
        /// <param name="changedFiles">List of changed file paths.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Confidence level (0.0 to 1.0).</returns>
        Task<double> GetAnalysisConfidenceAsync(List<string> changedFiles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Discovers test files in the project.
        /// </summary>
        /// <param name="projectRoot">Project root directory.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of discovered test file paths.</returns>
        Task<List<string>> DiscoverTestFilesAsync(string projectRoot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds a test dependency graph for the project.
        /// </summary>
        /// <param name="projectRoot">Project root directory.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Test dependency graph.</returns>
        Task<TestDependencyGraph> BuildDependencyGraphAsync(string projectRoot, CancellationToken cancellationToken = default);
    }
}