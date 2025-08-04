using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Interface for analyzing test dependencies and relationships.
    /// </summary>
    public interface ITestDependencyAnalyzer
    {
        /// <summary>
        /// Analyzes dependencies between test files.
        /// </summary>
        /// <param name="testFiles">List of test files to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of dependency rules.</returns>
        Task<List<TestDependencyRule>> AnalyzeDependenciesAsync(List<string> testFiles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects shared resources between tests.
        /// </summary>
        /// <param name="testFiles">List of test files to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of resource dependencies.</returns>
        Task<List<TestDependencyRule>> DetectResourceDependenciesAsync(List<string> testFiles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects data dependencies between tests.
        /// </summary>
        /// <param name="testFiles">List of test files to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of data dependencies.</returns>
        Task<List<TestDependencyRule>> DetectDataDependenciesAsync(List<string> testFiles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates dependency rules for consistency.
        /// </summary>
        /// <param name="dependencies">Dependency rules to validate.</param>
        /// <returns>Validation result.</returns>
        TestDependencyValidation ValidateDependencies(List<TestDependencyRule> dependencies);
    }

    /// <summary>
    /// Validation result for test dependencies.
    /// </summary>
    public class TestDependencyValidation
    {
        /// <summary>
        /// Whether the dependencies are valid.
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

        /// <summary>
        /// Detected circular dependencies.
        /// </summary>
        public List<List<string>> CircularDependencies { get; set; } = new List<List<string>>();
    }
} 