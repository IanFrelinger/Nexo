using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StandaloneTestRunner.AI;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Test suite for AI Services - orchestrates specialized test classes
    /// Tests AI Providers, Engines, Performance Monitor, Safety Validator, and other AI services
    /// </summary>
    public class AIServicesTests
    {
        private readonly bool _verbose;
        private readonly ProviderTests _providerTests;
        private readonly EngineTests _engineTests;
        private readonly PerformanceTests _performanceTests;
        private readonly SafetyTests _safetyTests;
        private readonly UsageTests _usageTests;
        private readonly ModelTests _modelTests;
        private readonly AnalyticsTests _analyticsTests;
        private readonly DistributedTests _distributedTests;
        private readonly CacheTests _cacheTests;
        private readonly RollbackTests _rollbackTests;
        private readonly RuntimeTests _runtimeTests;
        private readonly PipelineTests _pipelineTests;
        private readonly IntegrationTests _integrationTests;

        public AIServicesTests(bool verbose = false)
        {
            _verbose = verbose;
            _providerTests = new ProviderTests(verbose);
            _engineTests = new EngineTests(verbose);
            _performanceTests = new PerformanceTests(verbose);
            _safetyTests = new SafetyTests(verbose);
            _usageTests = new UsageTests(verbose);
            _modelTests = new ModelTests(verbose);
            _analyticsTests = new AnalyticsTests(verbose);
            _distributedTests = new DistributedTests(verbose);
            _cacheTests = new CacheTests(verbose);
            _rollbackTests = new RollbackTests(verbose);
            _runtimeTests = new RuntimeTests(verbose);
            _pipelineTests = new PipelineTests(verbose);
            _integrationTests = new IntegrationTests(verbose);
        }

        /// <summary>
        /// Discovers all AI Services tests by delegating to specialized test classes
        /// </summary>
        public List<TestInfo> DiscoverAIServicesTests()
        {
            var allTests = new List<TestInfo>();
            
            allTests.AddRange(_providerTests.DiscoverProviderTests());
            allTests.AddRange(_engineTests.DiscoverEngineTests());
            allTests.AddRange(_performanceTests.DiscoverPerformanceTests());
            allTests.AddRange(_safetyTests.DiscoverSafetyTests());
            allTests.AddRange(_usageTests.DiscoverUsageTests());
            allTests.AddRange(_modelTests.DiscoverModelTests());
            allTests.AddRange(_analyticsTests.DiscoverAnalyticsTests());
            allTests.AddRange(_distributedTests.DiscoverDistributedTests());
            allTests.AddRange(_cacheTests.DiscoverCacheTests());
            allTests.AddRange(_rollbackTests.DiscoverRollbackTests());
            allTests.AddRange(_runtimeTests.DiscoverRuntimeTests());
            allTests.AddRange(_pipelineTests.DiscoverPipelineTests());
            allTests.AddRange(_integrationTests.DiscoverIntegrationTests());
            
            return allTests;
        }

        /// <summary>
        /// Executes a specific AI Services test by ID by delegating to specialized test classes
        /// </summary>
        public bool ExecuteAIServicesTest(string testId)
        {
            // Delegate to appropriate specialized test class based on test ID
            if (testId.StartsWith("ai-provider-"))
            {
                return _providerTests.ExecuteProviderTest(testId);
            }
            else if (testId.StartsWith("ai-engine-"))
            {
                return _engineTests.ExecuteEngineTest(testId);
            }
            else if (testId.StartsWith("ai-performance-"))
            {
                return _performanceTests.ExecutePerformanceTest(testId);
            }
            else if (testId.StartsWith("ai-safety-"))
            {
                return _safetyTests.ExecuteSafetyTest(testId);
            }
            else if (testId.StartsWith("ai-usage-"))
            {
                return _usageTests.ExecuteUsageTest(testId);
            }
            else if (testId.StartsWith("ai-model-"))
            {
                return _modelTests.ExecuteModelTest(testId);
            }
            else if (testId.StartsWith("ai-advanced-analytics-"))
            {
                return _analyticsTests.ExecuteAnalyticsTest(testId);
            }
            else if (testId.StartsWith("ai-distributed-"))
            {
                return _distributedTests.ExecuteDistributedTest(testId);
            }
            else if (testId.StartsWith("ai-advanced-cache-"))
            {
                return _cacheTests.ExecuteCacheTest(testId);
            }
            else if (testId.StartsWith("ai-operation-rollback-"))
            {
                return _rollbackTests.ExecuteRollbackTest(testId);
            }
            else if (testId.StartsWith("ai-runtime-"))
            {
                return _runtimeTests.ExecuteRuntimeTest(testId);
            }
            else if (testId.StartsWith("ai-pipeline-"))
            {
                return _pipelineTests.ExecutePipelineTest(testId);
            }
            else if (testId.StartsWith("ai-integration-") || testId.StartsWith("ai-performance-test") || testId.StartsWith("ai-security-test"))
            {
                return _integrationTests.ExecuteIntegrationTest(testId);
            }
            else
            {
                throw new InvalidOperationException($"Unknown AI Services test: {testId}");
            }
        }
    }
}
