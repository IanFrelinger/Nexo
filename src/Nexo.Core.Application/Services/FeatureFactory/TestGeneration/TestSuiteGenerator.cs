using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using Nexo.Core.Domain.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.FeatureFactory.TestGeneration.Generators;

namespace Nexo.Core.Application.Services.FeatureFactory.TestGeneration
{
    /// <summary>
    /// Orchestrator for test suite generation that delegates to specialized generators.
    /// </summary>
    public partial class TestSuiteGenerator : ITestSuiteGenerator
    {
        private readonly ILogger<TestSuiteGenerator> _logger;
        private readonly UnitTestGenerator _unitTestGenerator;
        private readonly IntegrationTestGenerator _integrationTestGenerator;
        private readonly PerformanceTestGenerator _performanceTestGenerator;
        private readonly SecurityTestGenerator _securityTestGenerator;

        public TestSuiteGenerator(ILogger<TestSuiteGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitTestGenerator = new UnitTestGenerator(logger);
            _integrationTestGenerator = new IntegrationTestGenerator(logger);
            _performanceTestGenerator = new PerformanceTestGenerator(logger);
            _securityTestGenerator = new SecurityTestGenerator(logger);
        }

        public async Task<TestSuiteResult> GenerateTestSuiteAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating complete test suite for domain logic with {EntityCount} entities", domainLogic.Entities.Count);

                var result = new TestSuiteResult
                {
                    Success = true,
                    GeneratedAt = DateTime.UtcNow
                };

                // Generate unit tests
                var unitTests = await _unitTestGenerator.GenerateUnitTestsAsync(domainLogic, cancellationToken);
                result.UnitTests.AddRange(unitTests);

                // Generate integration tests
                var integrationTests = await _integrationTestGenerator.GenerateIntegrationTestsAsync(domainLogic, cancellationToken);
                result.IntegrationTests.AddRange(integrationTests);

                // Generate performance tests
                var performanceTests = await _performanceTestGenerator.GeneratePerformanceTestsAsync(domainLogic, cancellationToken);
                result.PerformanceTests.AddRange(performanceTests);

                // Generate security tests
                var securityTests = await _securityTestGenerator.GenerateSecurityTestsAsync(domainLogic, cancellationToken);
                result.SecurityTests.AddRange(securityTests);

                result.Success = true;
                result.Message = "Test suite generated successfully";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test suite");
                return new TestSuiteResult
                {
                    Success = false,
                    Message = $"Test suite generation failed: {ex.Message}",
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }
    }
}
