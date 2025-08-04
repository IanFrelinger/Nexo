using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Interface for automatic test suite generation from domain logic
    /// </summary>
    public interface ITestSuiteGenerator
    {
        /// <summary>
        /// Generates comprehensive unit tests for domain logic
        /// </summary>
        /// <param name="domainLogic">The domain logic to generate tests for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated unit test suite</returns>
        Task<UnitTestSuiteResult> GenerateUnitTestsAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates integration tests for domain logic
        /// </summary>
        /// <param name="domainLogic">The domain logic to generate tests for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated integration test suite</returns>
        Task<IntegrationTestSuiteResult> GenerateIntegrationTestsAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Identifies and generates edge case tests
        /// </summary>
        /// <param name="domainLogic">The domain logic to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated edge case test suite</returns>
        Task<EdgeCaseTestSuiteResult> GenerateEdgeCaseTestsAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates test coverage and generates additional tests if needed
        /// </summary>
        /// <param name="testSuite">The complete test suite to validate</param>
        /// <param name="domainLogic">The domain logic being tested</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Test coverage validation result</returns>
        Task<TestCoverageValidationResult> ValidateTestCoverageAsync(
            CompleteTestSuite testSuite,
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a complete test suite with all test types
        /// </summary>
        /// <param name="domainLogic">The domain logic to generate tests for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Complete test suite with unit, integration, and edge case tests</returns>
        Task<CompleteTestSuiteResult> GenerateCompleteTestSuiteAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates realistic test data for domain entities
        /// </summary>
        /// <param name="domainLogic">The domain logic to generate test data for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated test data suite</returns>
        Task<TestDataSuiteResult> GenerateTestDataAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates performance tests for domain logic
        /// </summary>
        /// <param name="domainLogic">The domain logic to generate performance tests for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated performance test suite</returns>
        Task<PerformanceTestSuiteResult> GeneratePerformanceTestsAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates security-focused tests for domain logic
        /// </summary>
        /// <param name="domainLogic">The domain logic to generate security tests for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated security test suite</returns>
        Task<SecurityTestSuiteResult> GenerateSecurityTestsAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates accessibility tests for domain logic
        /// </summary>
        /// <param name="domainLogic">The domain logic to generate accessibility tests for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated accessibility test suite</returns>
        Task<AccessibilityTestSuiteResult> GenerateAccessibilityTestsAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);
    }
}