using Nexo.Feature.Factory.Testing.Progress;

namespace Nexo.Feature.Factory.Testing.Coverage
{
    /// <summary>
    /// Interface for analyzing test coverage.
    /// </summary>
    public interface ITestCoverageAnalyzer
    {
        /// <summary>
        /// Analyzes test coverage for the specified assemblies.
        /// </summary>
        /// <param name="assemblyPaths">Paths to the assemblies to analyze</param>
        /// <param name="testAssemblyPaths">Paths to the test assemblies</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Test coverage information</returns>
        Task<TestCoverageInfo> AnalyzeCoverageAsync(
            IEnumerable<string> assemblyPaths,
            IEnumerable<string> testAssemblyPaths,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes test coverage for the specified source files.
        /// </summary>
        /// <param name="sourceFiles">Paths to the source files to analyze</param>
        /// <param name="testFiles">Paths to the test files</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Test coverage information</returns>
        Task<TestCoverageInfo> AnalyzeSourceCoverageAsync(
            IEnumerable<string> sourceFiles,
            IEnumerable<string> testFiles,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a coverage report in the specified format.
        /// </summary>
        /// <param name="coverage">The coverage information</param>
        /// <param name="outputPath">The output path for the report</param>
        /// <param name="format">The report format</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task GenerateCoverageReportAsync(
            TestCoverageInfo coverage,
            string outputPath,
            CoverageReportFormat format,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets coverage thresholds for different coverage types.
        /// </summary>
        /// <returns>Coverage thresholds</returns>
        CoverageThresholds GetCoverageThresholds();

        /// <summary>
        /// Sets coverage thresholds for different coverage types.
        /// </summary>
        /// <param name="thresholds">The coverage thresholds</param>
        void SetCoverageThresholds(CoverageThresholds thresholds);
    }

    /// <summary>
    /// Represents coverage report formats.
    /// </summary>
    public enum CoverageReportFormat
    {
        /// <summary>
        /// HTML format
        /// </summary>
        Html,

        /// <summary>
        /// JSON format
        /// </summary>
        Json,

        /// <summary>
        /// XML format
        /// </summary>
        Xml,

        /// <summary>
        /// Text format
        /// </summary>
        Text,

        /// <summary>
        /// Markdown format
        /// </summary>
        Markdown
    }

    /// <summary>
    /// Represents coverage thresholds for different coverage types.
    /// </summary>
    public sealed class CoverageThresholds
    {
        /// <summary>
        /// Gets or sets the overall coverage threshold.
        /// </summary>
        public double OverallCoverage { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets the line coverage threshold.
        /// </summary>
        public double LineCoverage { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets the branch coverage threshold.
        /// </summary>
        public double BranchCoverage { get; set; } = 70.0;

        /// <summary>
        /// Gets or sets the method coverage threshold.
        /// </summary>
        public double MethodCoverage { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets the class coverage threshold.
        /// </summary>
        public double ClassCoverage { get; set; } = 90.0;

        /// <summary>
        /// Gets or sets whether to fail if thresholds are not met.
        /// </summary>
        public bool FailOnThreshold { get; set; } = true;
    }
}
