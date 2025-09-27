using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StandaloneTestRunner.FeatureFactory;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Test suite for Feature Factory Deployment & Integration services
    /// Uses smaller, focused test classes for better maintainability
    /// </summary>
    public partial class FeatureFactoryDeploymentTests
    {
        private readonly bool _verbose;
        private readonly DeploymentManagerTests _deploymentManagerTests;
        private readonly SystemIntegratorTests _systemIntegratorTests;
        private readonly ApplicationMonitorTests _applicationMonitorTests;

        public FeatureFactoryDeploymentTestsRefactored(bool verbose = false)
        {
            _verbose = verbose;
            _deploymentManagerTests = new DeploymentManagerTests(verbose);
            _systemIntegratorTests = new SystemIntegratorTests(verbose);
            _applicationMonitorTests = new ApplicationMonitorTests(verbose);
        }

        /// <summary>
        /// Discovers all Feature Factory Deployment & Integration tests
        /// </summary>
        public List<TestInfo> DiscoverFeatureFactoryDeploymentTests()
        {
            var tests = new List<TestInfo>();
            
            // Add deployment manager tests
            tests.AddRange(_deploymentManagerTests.DiscoverDeploymentManagerTests());
            
            // Add system integrator tests
            tests.AddRange(_systemIntegratorTests.DiscoverSystemIntegratorTests());
            
            // Add application monitor tests
            tests.AddRange(_applicationMonitorTests.DiscoverApplicationMonitorTests());
            
            return tests;
        }

        /// <summary>
        /// Runs Feature Factory Deployment & Integration tests
        /// </summary>
        public async Task<TestResult> RunFeatureFactoryDeploymentTestsAsync(TestInfo testInfo, CancellationToken cancellationToken = default)
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
                if (testInfo.Name.StartsWith("deployment-manager"))
                {
                    result = await _deploymentManagerTests.RunDeploymentManagerTestsAsync(testInfo, cancellationToken);
                }
                else if (testInfo.Name.StartsWith("system-integrator"))
                {
                    result = await _systemIntegratorTests.RunSystemIntegratorTestsAsync(testInfo, cancellationToken);
                }
                else if (testInfo.Name.StartsWith("application-monitor"))
                {
                    result = await _applicationMonitorTests.RunApplicationMonitorTestsAsync(testInfo, cancellationToken);
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
        /// Runs all Feature Factory Deployment & Integration tests
        /// </summary>
        public async Task<List<TestResult>> RunAllFeatureFactoryDeploymentTestsAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<TestResult>();
            var tests = DiscoverFeatureFactoryDeploymentTests();

            foreach (var test in tests)
            {
                var result = await RunFeatureFactoryDeploymentTestsAsync(test, cancellationToken);
                results.Add(result);
            }

            return results;
        }
    }
}
