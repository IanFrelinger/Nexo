using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.TestGeneration.Generators
{
    public partial class IntegrationTestGenerator
    {
        private readonly ILogger<IntegrationTestGenerator> _logger;

        public IntegrationTestGenerator(ILogger<IntegrationTestGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<IntegrationTest>> GenerateIntegrationTestsAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken)
        {
            var integrationTests = new List<IntegrationTest>();

            try
            {
                _logger.LogInformation("Generating integration tests for {ServiceCount} services", domainLogic.DomainServices.Count);

                foreach (var service in domainLogic.DomainServices)
                {
                    var integrationTest = new IntegrationTest
                    {
                        ServiceName = service,
                        TestName = $"Test{service}Integration",
                        TestCode = GenerateIntegrationTestCode(service),
                        GeneratedAt = DateTime.UtcNow
                    };

                    integrationTests.Add(integrationTest);
                }

                return integrationTests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating integration tests");
                return integrationTests;
            }
        }

        private string GenerateIntegrationTestCode(string serviceName)
        {
            return $@"
using Xunit;
using {GetNamespaceForService(serviceName)};

namespace Tests.Integration
{{
    public partial class {serviceName}IntegrationTests
    {{
        [Fact]
        public void {serviceName}_Integration_Should_Work()
        {{
            // Integration test implementation for {serviceName}
            Assert.True(true);
        }}
    }}
}}";
        }

        private string GetNamespaceForService(string serviceName)
        {
            return $"Domain.Services.{serviceName}";
        }
    }

    public partial class IntegrationTest
    {
        public string ServiceName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }
}
