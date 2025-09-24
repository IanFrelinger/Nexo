using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StandaloneTestRunner.FeatureFactory;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Test suite for Feature Factory Application Logic services
    /// Uses smaller, focused test classes for better maintainability
    /// </summary>
    public class FeatureFactoryApplicationTests
    {
        private readonly bool _verbose;
        private readonly ApplicationLogicGeneratorTests _applicationLogicGeneratorTests;
        private readonly FrameworkAdapterTests _frameworkAdapterTests;

        public FeatureFactoryApplicationTestsRefactored(bool verbose = false)
        {
            _verbose = verbose;
            _applicationLogicGeneratorTests = new ApplicationLogicGeneratorTests(verbose);
            _frameworkAdapterTests = new FrameworkAdapterTests(verbose);
        }

        /// <summary>
        /// Discovers all Feature Factory Application Logic tests
        /// </summary>
        public List<TestInfo> DiscoverFeatureFactoryApplicationTests()
        {
            var tests = new List<TestInfo>();
            
            // Add application logic generator tests
            tests.AddRange(_applicationLogicGeneratorTests.DiscoverApplicationLogicGeneratorTests());
            
            // Add framework adapter tests
            tests.AddRange(_frameworkAdapterTests.DiscoverFrameworkAdapterTests());
            
            return tests;
        }

        /// <summary>
        /// Runs Feature Factory Application Logic tests
        /// </summary>
        public async Task<TestResult> RunFeatureFactoryApplicationTestsAsync(TestInfo testInfo, CancellationToken cancellationToken = default)
        {
            var result = new TestResult
            {
                TestName = testInfo.Name,
                Success = false,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Route to appropriate test class based on test name
                if (testInfo.Name.StartsWith("application-logic-generator"))
                {
                    result = await _applicationLogicGeneratorTests.RunApplicationLogicGeneratorTestsAsync(testInfo, cancellationToken);
                }
                else if (testInfo.Name.StartsWith("framework-adapter"))
                {
                    result = await _frameworkAdapterTests.RunFrameworkAdapterTestsAsync(testInfo, cancellationToken);
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"Unknown test category: {testInfo.Name}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
            }

            return result;
        }

        /// <summary>
        /// Runs all Feature Factory Application Logic tests
        /// </summary>
        public async Task<List<TestResult>> RunAllFeatureFactoryApplicationTestsAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<TestResult>();
            var tests = DiscoverFeatureFactoryApplicationTests();

            foreach (var test in tests)
            {
                var result = await RunFeatureFactoryApplicationTestsAsync(test, cancellationToken);
                results.Add(result);
            }

            return results;
        }
    }
}
