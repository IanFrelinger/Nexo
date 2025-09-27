using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Test execution functionality for TestAggregator.
    /// Handles test execution, timeout protection, and result management.
    /// </summary>
    public partial class TestAggregator
    {
        /// <summary>
        /// Executes a single test with timeout protection.
        /// </summary>
        private async Task<TestResult> ExecuteTestAsync(TestInfo testInfo)
        {
            var startTime = DateTimeOffset.UtcNow;

            try
            {
                if (_verbose)
                {
                    Console.WriteLine($"Executing test: {testInfo.TestId} with timeout: {testInfo.Timeout}s");
                }

                // Create aggressive timeout if enabled
                using var timeoutCts = new CancellationTokenSource();
                if (_forceTimeout)
                {
                    // Use more aggressive timeout
                    var aggressiveTimeout = Math.Min(testInfo.Timeout, 3); // Cap at 3 seconds for aggressive mode
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(aggressiveTimeout));
                }
                else
                {
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(testInfo.Timeout));
                }

                // Execute the test method with timeout monitoring
                var testExecutionTask = Task.Run(() => InvokeTestMethod(testInfo.TestId), timeoutCts.Token);

                try
                {
                    var result = await testExecutionTask;
                    var endTime = DateTimeOffset.UtcNow;
                    var duration = endTime - startTime;

                    if (_verbose)
                    {
                        Console.WriteLine($"Test {testInfo.TestId} completed: {result} ({duration.TotalMilliseconds}ms)");
                    }

                    return new TestResult(testInfo.TestId, result, duration, null);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    var endTime = DateTimeOffset.UtcNow;
                    var duration = endTime - startTime;

                    if (_verbose)
                    {
                        Console.WriteLine($"Test {testInfo.TestId} timed out after {duration.TotalMilliseconds}ms");
                    }

                    return new TestResult(testInfo.TestId, false, duration,
                        $"Test timed out after {testInfo.Timeout} seconds");
                }
            }
            catch (Exception ex)
            {
                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;

                if (_verbose)
                {
                    Console.WriteLine($"Test {testInfo.TestId} failed: {ex.Message}");
                }

                return new TestResult(testInfo.TestId, false, duration, ex.Message);
            }
        }

        /// <summary>
        /// Invokes the appropriate test method based on test ID.
        /// </summary>
        private bool InvokeTestMethod(string testId)
        {
            return testId switch
            {
                "aggregator-basic-validation" => RunBasicValidationTest(),
                "aggregator-configuration-test" => RunConfigurationTest(),
                "aggregator-timeout-test" => RunTimeoutTest(),
                "aggregator-performance-test" => RunPerformanceTest(),
                "aggregator-integration-test" => RunIntegrationTest(),
                "aggregator-security-test" => RunSecurityTest(),
                _ when testId.StartsWith("logging-") => RunLoggingTest(testId), // Handle logging tests
                _ when testId.StartsWith("epic5_4-phase") => RunEpic5_4EnhancedTest(testId), // Handle Enhanced Epic 5.4 tests
                _ when testId.StartsWith("epic5_4-") => RunEpic5_4Test(testId), // Handle Epic 5.4 tests
                _ when testId.StartsWith("feature-factory-domain-logic-") => RunFeatureFactoryDomainTest(testId), // Handle Feature Factory Domain Logic tests
                _ when testId.StartsWith("feature-factory-application-logic-") => RunFeatureFactoryApplicationTest(testId), // Handle Feature Factory Application Logic tests
                _ when testId.StartsWith("feature-factory-framework-adapter-") => RunFeatureFactoryApplicationTest(testId), // Handle Feature Factory Framework Adapter tests
                _ when testId.StartsWith("feature-factory-deployment-") => RunFeatureFactoryDeploymentTest(testId), // Handle Feature Factory Deployment tests
                _ when testId.StartsWith("feature-factory-system-integrator-") => RunFeatureFactoryDeploymentTest(testId), // Handle Feature Factory System Integrator tests
                _ when testId.StartsWith("feature-factory-application-monitor-") => RunFeatureFactoryDeploymentTest(testId), // Handle Feature Factory Application Monitor tests
                _ when testId.StartsWith("feature-factory-deployment-orchestrator-") => RunFeatureFactoryDeploymentTest(testId), // Handle Feature Factory Deployment Orchestrator tests
                _ when testId.StartsWith("ai-") => RunAIServicesTest(testId), // Handle AI Services tests
                _ when testId.StartsWith("core-domain-") => RunCoreDomainEntitiesTest(testId), // Handle Core Domain Entities tests
                _ when testId.StartsWith("large-test-") => RunGenericTest(), // Handle large test collection
                _ when testId.StartsWith("smoke-") => RunGenericTest(), // Handle smoke tests
                _ => RunGenericTest() // Default fallback for any unknown test
            };
        }
    }
}
