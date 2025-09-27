using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Orchestrator for Feature Factory Domain Logic tests that delegates to specialized test classes.
    /// </summary>
    public partial class FeatureFactoryDomainTests
    {
        private readonly bool _verbose;
        private readonly DomainLogicGeneratorTests _domainLogicGeneratorTests;
        private readonly DomainLogicValidatorTests _domainLogicValidatorTests;
        private readonly DomainLogicOrchestratorTests _domainLogicOrchestratorTests;

        public FeatureFactoryDomainTests(bool verbose = false)
        {
            _verbose = verbose;
            _domainLogicGeneratorTests = new DomainLogicGeneratorTests(verbose);
            _domainLogicValidatorTests = new DomainLogicValidatorTests(verbose);
            _domainLogicOrchestratorTests = new DomainLogicOrchestratorTests(verbose);
        }

        /// <summary>
        /// Discovers all Feature Factory Domain Logic tests
        /// </summary>
        public List<TestInfo> DiscoverFeatureFactoryDomainTests()
        {
            var tests = new List<TestInfo>();
            
            tests.AddRange(_domainLogicGeneratorTests.DiscoverDomainLogicGeneratorTests());
            tests.AddRange(_domainLogicValidatorTests.DiscoverDomainLogicValidatorTests());
            tests.AddRange(_domainLogicOrchestratorTests.DiscoverDomainLogicOrchestratorTests());
            
            return tests;
        }

        /// <summary>
        /// Executes Feature Factory Domain Logic tests
        /// </summary>
        public async Task<TestResult> ExecuteFeatureFactoryDomainTestAsync(TestInfo testInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                if (testInfo.Category.StartsWith("DomainLogic-Generator"))
                {
                    return await _domainLogicGeneratorTests.ExecuteDomainLogicGeneratorTestAsync(testInfo, cancellationToken);
                }
                else if (testInfo.Category.StartsWith("DomainLogic-Validator"))
                {
                    return await _domainLogicValidatorTests.ExecuteDomainLogicValidatorTestAsync(testInfo, cancellationToken);
                }
                else if (testInfo.Category.StartsWith("DomainLogic-Orchestrator"))
                {
                    return await _domainLogicOrchestratorTests.ExecuteDomainLogicOrchestratorTestAsync(testInfo, cancellationToken);
                }
                else
                {
                    return new TestResult
                    {
                        TestName = testInfo.Name,
                        Success = false,
                        Message = $"Unknown test category: {testInfo.Category}",
                        ExecutionTime = TimeSpan.Zero
                    };
                }
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = testInfo.Name,
                    Success = false,
                    Message = $"Test execution failed: {ex.Message}",
                    ExecutionTime = TimeSpan.Zero
                };
            }
        }
    }
}
