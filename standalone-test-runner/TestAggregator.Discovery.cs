using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Test discovery functionality for TestAggregator.
    /// Handles test discovery and collection from various test suites.
    /// </summary>
    public partial class TestAggregator
    {
        /// <summary>
        /// Discovers and adds default tests to the aggregator.
        /// </summary>
        public void DiscoverDefaultTests()
        {
            var defaultTests = new List<TestInfo>
            {
                new TestInfo(
                    "aggregator-basic-validation",
                    "Basic Validation Test",
                    "Simple test that validates basic functionality",
                    "Unit",
                    "High",
                    2,
                    5,
                    new[] { "aggregator", "basic", "validation" }
                ),
                new TestInfo(
                    "aggregator-configuration-test",
                    "Configuration Test",
                    "Simple test that validates configuration loading",
                    "Unit",
                    "Medium",
                    1,
                    3,
                    new[] { "aggregator", "configuration" }
                ),
                new TestInfo(
                    "aggregator-timeout-test",
                    "Timeout Test",
                    "Simple test that validates timeout handling",
                    "Unit",
                    "High",
                    1,
                    3,
                    new[] { "aggregator", "timeout" }
                ),
                new TestInfo(
                    "aggregator-performance-test",
                    "Performance Test",
                    "Simple test that validates performance",
                    "Performance",
                    "Medium",
                    3,
                    8,
                    new[] { "aggregator", "performance" }
                ),
                new TestInfo(
                    "aggregator-integration-test",
                    "Integration Test",
                    "Test that validates component integration",
                    "Integration",
                    "High",
                    2,
                    6,
                    new[] { "aggregator", "integration" }
                ),
                new TestInfo(
                    "aggregator-security-test",
                    "Security Test",
                    "Test that validates security measures",
                    "Security",
                    "Critical",
                    1,
                    4,
                    new[] { "aggregator", "security" }
                )
            };

            // Add logging tests
            var loggingTestSuite = new LoggingTestSuite(_verbose);
            var loggingTests = loggingTestSuite.DiscoverLoggingTests();
            defaultTests.AddRange(loggingTests);

            // Add Epic 5.4 tests
            var epic5_4TestSuite = new Epic5_4TestSuite(_verbose);
            var epic5_4Tests = epic5_4TestSuite.DiscoverEpic5_4Tests();
            defaultTests.AddRange(epic5_4Tests);

            // Add Enhanced Epic 5.4 tests
            var epic5_4EnhancedTestSuite = new Epic5_4EnhancedTestSuite(_verbose);
            var epic5_4EnhancedTests = epic5_4EnhancedTestSuite.DiscoverEnhancedEpic5_4Tests();
            defaultTests.AddRange(epic5_4EnhancedTests);

            // Add Feature Factory Domain Logic tests
            var featureFactoryDomainTests = new FeatureFactoryDomainTests(_verbose);
            var featureFactoryDomainTestList = featureFactoryDomainTests.DiscoverFeatureFactoryDomainTests();
            defaultTests.AddRange(featureFactoryDomainTestList);

            // Add Feature Factory Application Logic tests
            var featureFactoryApplicationTests = new FeatureFactoryApplicationTests(_verbose);
            var featureFactoryApplicationTestList = featureFactoryApplicationTests.DiscoverFeatureFactoryApplicationTests();
            defaultTests.AddRange(featureFactoryApplicationTestList);

            // Add Core Domain Entities tests
            var coreDomainEntitiesTests = new CoreDomainEntitiesTests(_verbose);
            var coreDomainEntitiesTestList = coreDomainEntitiesTests.DiscoverCoreDomainEntitiesTests();
            defaultTests.AddRange(coreDomainEntitiesTestList);

            // Add Feature Factory Deployment tests
            var featureFactoryDeploymentTests = new FeatureFactoryDeploymentTests(_verbose);
            var featureFactoryDeploymentTestList = featureFactoryDeploymentTests.DiscoverFeatureFactoryDeploymentTests();
            defaultTests.AddRange(featureFactoryDeploymentTestList);

            // Add AI Services tests
            var aiServicesTests = new AIServicesTests(_verbose);
            var aiServicesTestList = aiServicesTests.DiscoverAIServicesTests();
            defaultTests.AddRange(aiServicesTestList);

            AddTests(defaultTests);
            
            if (_verbose)
            {
                Console.WriteLine($"Discovered and added {defaultTests.Count} default tests ({loggingTests.Count} logging tests, {epic5_4Tests.Count} Epic 5.4 tests, {epic5_4EnhancedTests.Count} Enhanced Epic 5.4 tests, {featureFactoryDomainTestList.Count} Feature Factory Domain tests, {featureFactoryApplicationTestList.Count} Feature Factory Application tests, {coreDomainEntitiesTestList.Count} Core Domain Entities tests, {featureFactoryDeploymentTestList.Count} Feature Factory Deployment tests, {aiServicesTestList.Count} AI Services tests)");
            }
        }
    }
}
