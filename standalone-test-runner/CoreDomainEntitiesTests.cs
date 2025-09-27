using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StandaloneTestRunner.CoreDomain;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Test suite for Core Domain Entities
    /// Uses smaller, focused test classes for better maintainability
    /// </summary>
    public partial class CoreDomainEntitiesTests
    {
        private readonly bool _verbose;
        private readonly DomainEntityTests _domainEntityTests;
        private readonly ValueObjectTests _valueObjectTests;
        private readonly BusinessRuleTests _businessRuleTests;

        public CoreDomainEntitiesTestsRefactored(bool verbose = false)
        {
            _verbose = verbose;
            _domainEntityTests = new DomainEntityTests(verbose);
            _valueObjectTests = new ValueObjectTests(verbose);
            _businessRuleTests = new BusinessRuleTests(verbose);
        }

        /// <summary>
        /// Discovers all Core Domain Entities tests
        /// </summary>
        public List<TestInfo> DiscoverCoreDomainEntitiesTests()
        {
            var tests = new List<TestInfo>();
            
            // Add domain entity tests
            tests.AddRange(_domainEntityTests.DiscoverDomainEntityTests());
            
            // Add value object tests
            tests.AddRange(_valueObjectTests.DiscoverValueObjectTests());
            
            // Add business rule tests
            tests.AddRange(_businessRuleTests.DiscoverBusinessRuleTests());
            
            return tests;
        }

        /// <summary>
        /// Runs Core Domain Entities tests
        /// </summary>
        public async Task<TestResult> RunCoreDomainEntitiesTestsAsync(TestInfo testInfo, CancellationToken cancellationToken = default)
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
                if (testInfo.Name.StartsWith("domain-entity"))
                {
                    result = await _domainEntityTests.RunDomainEntityTestsAsync(testInfo, cancellationToken);
                }
                else if (testInfo.Name.StartsWith("value-object"))
                {
                    result = await _valueObjectTests.RunValueObjectTestsAsync(testInfo, cancellationToken);
                }
                else if (testInfo.Name.StartsWith("business-rule"))
                {
                    result = await _businessRuleTests.RunBusinessRuleTestsAsync(testInfo, cancellationToken);
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
        /// Runs all Core Domain Entities tests
        /// </summary>
        public async Task<List<TestResult>> RunAllCoreDomainEntitiesTestsAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<TestResult>();
            var tests = DiscoverCoreDomainEntitiesTests();

            foreach (var test in tests)
            {
                var result = await RunCoreDomainEntitiesTestsAsync(test, cancellationToken);
                results.Add(result);
            }

            return results;
        }
    }
}
