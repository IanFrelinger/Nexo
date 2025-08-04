using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Interface for intelligent test selection based on code changes and impact analysis.
    /// </summary>
    public interface ISmartTestSelector
    {
        /// <summary>
        /// Selects tests to run based on intelligent analysis of changes.
        /// </summary>
        /// <param name="options">Smart test selection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Smart test selection result.</returns>
        Task<SmartTestSelectionResult> SelectTestsAsync(SmartTestSelectionOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Selects tests based on specific changed files.
        /// </summary>
        /// <param name="changedFiles">List of changed file paths.</param>
        /// <param name="options">Smart test selection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Smart test selection result.</returns>
        Task<SmartTestSelectionResult> SelectTestsForChangedFilesAsync(List<string> changedFiles, SmartTestSelectionOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Selects tests based on Git changes since a reference.
        /// </summary>
        /// <param name="sinceReference">Git reference (commit, branch, etc.).</param>
        /// <param name="options">Smart test selection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Smart test selection result.</returns>
        Task<SmartTestSelectionResult> SelectTestsForGitChangesAsync(string sinceReference, SmartTestSelectionOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a summary of the smart test selection process.
        /// </summary>
        /// <param name="result">Smart test selection result.</param>
        /// <returns>Human-readable summary.</returns>
        string GetSelectionSummary(SmartTestSelectionResult result);

        /// <summary>
        /// Validates smart test selection options.
        /// </summary>
        /// <param name="options">Options to validate.</param>
        /// <returns>Validation result.</returns>
        SmartTestSelectionValidation ValidateOptions(SmartTestSelectionOptions options);
    }

    /// <summary>
    /// Options for smart test selection.
    /// </summary>
    public class SmartTestSelectionOptions
    {
        /// <summary>
        /// Minimum confidence level required for smart selection (0.0 to 1.0).
        /// </summary>
        public double MinimumConfidence { get; set; } = 0.7;

        /// <summary>
        /// Maximum percentage of tests to select (0.0 to 1.0).
        /// </summary>
        public double MaximumSelectionRatio { get; set; } = 0.8;

        /// <summary>
        /// Whether to include tests for unchanged files that might be affected.
        /// </summary>
        public bool IncludeIndirectDependencies { get; set; } = true;

        /// <summary>
        /// Whether to run all tests if confidence is too low.
        /// </summary>
        public bool FallbackToAllTests { get; set; } = true;

        /// <summary>
        /// Whether to include tests for configuration files.
        /// </summary>
        public bool IncludeConfigFileTests { get; set; } = true;

        /// <summary>
        /// Whether to include tests for infrastructure files.
        /// </summary>
        public bool IncludeInfrastructureTests { get; set; } = true;

        /// <summary>
        /// Specific test categories to include.
        /// </summary>
        public List<string> IncludeCategories { get; set; } = new List<string>();

        /// <summary>
        /// Specific test categories to exclude.
        /// </summary>
        public List<string> ExcludeCategories { get; set; } = new List<string>();

        /// <summary>
        /// Whether to use cached analysis results.
        /// </summary>
        public bool UseCache { get; set; } = true;

        /// <summary>
        /// Whether to force fresh analysis.
        /// </summary>
        public bool ForceRefresh { get; set; } = false;
    }

    /// <summary>
    /// Result of smart test selection.
    /// </summary>
    public class SmartTestSelectionResult
    {
        /// <summary>
        /// Selected test files to run.
        /// </summary>
        public List<string> SelectedTests { get; set; } = new List<string>();

        /// <summary>
        /// All available test files.
        /// </summary>
        public List<string> AllTests { get; set; } = new List<string>();

        /// <summary>
        /// Confidence level of the selection (0.0 to 1.0).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Whether the selection used smart analysis or fell back to all tests.
        /// </summary>
        public bool UsedSmartSelection { get; set; }

        /// <summary>
        /// Reason for the selection decision.
        /// </summary>
        public string SelectionReason { get; set; } = string.Empty;

        /// <summary>
        /// Detailed test impact analysis.
        /// </summary>
        public TestImpactAnalysis ImpactAnalysis { get; set; }

        /// <summary>
        /// Changed files that were analyzed.
        /// </summary>
        public List<string> ChangedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Warnings or issues with the selection.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Performance metrics for the selection process.
        /// </summary>
        public SmartTestSelectionMetrics Metrics { get; set; } = new SmartTestSelectionMetrics();
    }

    /// <summary>
    /// Performance metrics for smart test selection.
    /// </summary>
    public class SmartTestSelectionMetrics
    {
        /// <summary>
        /// Total time taken for selection in milliseconds.
        /// </summary>
        public long TotalTimeMs { get; set; }

        /// <summary>
        /// Time taken for Git analysis in milliseconds.
        /// </summary>
        public long GitAnalysisTimeMs { get; set; }

        /// <summary>
        /// Time taken for impact analysis in milliseconds.
        /// </summary>
        public long ImpactAnalysisTimeMs { get; set; }

        /// <summary>
        /// Time taken for test discovery in milliseconds.
        /// </summary>
        public long TestDiscoveryTimeMs { get; set; }

        /// <summary>
        /// Number of files analyzed.
        /// </summary>
        public int FilesAnalyzed { get; set; }

        /// <summary>
        /// Number of tests discovered.
        /// </summary>
        public int TestsDiscovered { get; set; }

        /// <summary>
        /// Number of tests selected.
        /// </summary>
        public int TestsSelected { get; set; }
    }

    /// <summary>
    /// Validation result for smart test selection options.
    /// </summary>
    public class SmartTestSelectionValidation
    {
        /// <summary>
        /// Whether the options are valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}