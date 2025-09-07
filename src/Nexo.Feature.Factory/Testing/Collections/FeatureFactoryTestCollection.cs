using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Attributes;
using Nexo.Feature.Factory.Testing.Fixtures;
using Nexo.Feature.Factory.Testing.Models;
using Nexo.Feature.Factory.Testing.Runner;

namespace Nexo.Feature.Factory.Testing.Collections
{
    /// <summary>
    /// Test collection for Feature Factory tests.
    /// </summary>
    [TestClass(
        DisplayName = "Feature Factory Test Collection",
        Description = "Collection of all Feature Factory related tests",
        Category = TestCategory.Functional,
        Tags = new[] { "feature-factory", "collection", "comprehensive" }
    )]
    public sealed class FeatureFactoryTestCollection
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FeatureFactoryTestCollection> _logger;

        /// <summary>
        /// Initializes a new instance of the FeatureFactoryTestCollection class.
        /// </summary>
        public FeatureFactoryTestCollection(IServiceProvider serviceProvider, ILogger<FeatureFactoryTestCollection> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs all critical tests for Feature Factory.
        /// </summary>
        [EndToEndTest(
            DisplayName = "Run Critical Feature Factory Tests",
            Description = "Runs all critical tests for Feature Factory functionality",
            EstimatedDurationSeconds = 600,
            TimeoutSeconds = 1200,
            Priority = TestPriority.Critical,
            Tags = new[] { "critical", "comprehensive", "e2e" }
        )]
        public async Task<bool> RunCriticalTestsAsync()
        {
            _logger.LogInformation("Running critical Feature Factory tests...");

            try
            {
                var testFixture = new FeatureFactoryTestFixture(_serviceProvider, 
                    _serviceProvider.GetRequiredService<ILogger<FeatureFactoryTestFixture>>());

                var results = new List<bool>();

                // Run AI connectivity test
                _logger.LogInformation("Running AI connectivity test...");
                var aiResult = await testFixture.ValidateAiConnectivityAsync();
                results.Add(aiResult);

                if (!aiResult)
                {
                    _logger.LogError("AI connectivity test failed, skipping remaining tests");
                    return false;
                }

                // Run domain analysis test
                _logger.LogInformation("Running domain analysis test...");
                var domainResult = await testFixture.ValidateDomainAnalysisAsync();
                results.Add(domainResult);

                // Run code generation test
                _logger.LogInformation("Running code generation test...");
                var codeResult = await testFixture.ValidateCodeGenerationAsync();
                results.Add(codeResult);

                // Run end-to-end workflow test
                _logger.LogInformation("Running end-to-end workflow test...");
                var e2eResult = await testFixture.ValidateEndToEndWorkflowAsync();
                results.Add(e2eResult);

                var successCount = results.Count(r => r);
                var totalCount = results.Count;
                var successRate = (double)successCount / totalCount * 100;

                _logger.LogInformation("Critical tests completed: {SuccessCount}/{TotalCount} passed ({SuccessRate:F1}%)", 
                    successCount, totalCount, successRate);

                return successRate >= 75.0; // Require at least 75% success rate
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical tests failed");
                return false;
            }
        }

        /// <summary>
        /// Runs all performance tests for Feature Factory.
        /// </summary>
        [PerformanceTest(
            DisplayName = "Run Performance Tests",
            Description = "Runs all performance-related tests for Feature Factory",
            EstimatedDurationSeconds = 300,
            TimeoutSeconds = 600,
            Priority = TestPriority.Medium,
            Tags = new[] { "performance", "metrics", "benchmark" }
        )]
        public async Task<bool> RunPerformanceTestsAsync()
        {
            _logger.LogInformation("Running performance tests...");

            try
            {
                var testFixture = new FeatureFactoryTestFixture(_serviceProvider, 
                    _serviceProvider.GetRequiredService<ILogger<FeatureFactoryTestFixture>>());

                // Run performance metrics test
                var performanceResult = await testFixture.ValidatePerformanceMetricsAsync();

                _logger.LogInformation("Performance tests completed: {Result}", 
                    performanceResult ? "PASSED" : "FAILED");

                return performanceResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Performance tests failed");
                return false;
            }
        }

        /// <summary>
        /// Runs all validation tests for Feature Factory.
        /// </summary>
        [ValidationTest(
            DisplayName = "Run Validation Tests",
            Description = "Runs all validation and error handling tests",
            EstimatedDurationSeconds = 60,
            TimeoutSeconds = 120,
            Priority = TestPriority.Medium,
            Tags = new[] { "validation", "error-handling", "robustness" }
        )]
        public async Task<bool> RunValidationTestsAsync()
        {
            _logger.LogInformation("Running validation tests...");

            try
            {
                var testFixture = new FeatureFactoryTestFixture(_serviceProvider, 
                    _serviceProvider.GetRequiredService<ILogger<FeatureFactoryTestFixture>>());

                // Run error handling test
                var validationResult = await testFixture.ValidateErrorHandlingAsync();

                _logger.LogInformation("Validation tests completed: {Result}", 
                    validationResult ? "PASSED" : "FAILED");

                return validationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation tests failed");
                return false;
            }
        }

        /// <summary>
        /// Runs a comprehensive test suite for Feature Factory.
        /// </summary>
        [EndToEndTest(
            DisplayName = "Run Comprehensive Test Suite",
            Description = "Runs all tests for comprehensive Feature Factory validation",
            EstimatedDurationSeconds = 900,
            TimeoutSeconds = 1800,
            Priority = TestPriority.Critical,
            Tags = new[] { "comprehensive", "full-suite", "e2e" }
        )]
        public async Task<bool> RunComprehensiveTestSuiteAsync()
        {
            _logger.LogInformation("Running comprehensive test suite...");

            try
            {
                var results = new List<bool>();

                // Run critical tests
                _logger.LogInformation("Running critical tests...");
                var criticalResult = await RunCriticalTestsAsync();
                results.Add(criticalResult);

                // Run performance tests
                _logger.LogInformation("Running performance tests...");
                var performanceResult = await RunPerformanceTestsAsync();
                results.Add(performanceResult);

                // Run validation tests
                _logger.LogInformation("Running validation tests...");
                var validationResult = await RunValidationTestsAsync();
                results.Add(validationResult);

                var successCount = results.Count(r => r);
                var totalCount = results.Count;
                var successRate = (double)successCount / totalCount * 100;

                _logger.LogInformation("Comprehensive test suite completed: {SuccessCount}/{TotalCount} passed ({SuccessRate:F1}%)", 
                    successCount, totalCount, successRate);

                return successRate >= 80.0; // Require at least 80% success rate for comprehensive suite
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comprehensive test suite failed");
                return false;
            }
        }

        /// <summary>
        /// Setup method for test collection initialization.
        /// </summary>
        [TestSetup]
        public async Task SetupAsync()
        {
            _logger.LogInformation("Setting up Feature Factory test collection...");
            
            // Initialize test collection environment
            await Task.Delay(50); // Simulate setup work
            
            _logger.LogInformation("Feature Factory test collection setup completed");
        }

        /// <summary>
        /// Cleanup method for test collection teardown.
        /// </summary>
        [TestCleanup]
        public async Task CleanupAsync()
        {
            _logger.LogInformation("Cleaning up Feature Factory test collection...");
            
            // Clean up test collection environment
            await Task.Delay(50); // Simulate cleanup work
            
            _logger.LogInformation("Feature Factory test collection cleanup completed");
        }
    }
}
