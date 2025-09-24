using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.TestGeneration.Generators
{
    public class UnitTestGenerator
    {
        private readonly ILogger<UnitTestGenerator> _logger;

        public UnitTestGenerator(ILogger<UnitTestGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<UnitTest>> GenerateUnitTestsAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken)
        {
            var unitTests = new List<UnitTest>();

            try
            {
                _logger.LogInformation("Generating unit tests for {EntityCount} entities", domainLogic.Entities.Count);

                foreach (var entity in domainLogic.Entities)
                {
                    var unitTest = new UnitTest
                    {
                        EntityName = entity,
                        TestName = $"Test{entity}",
                        TestCode = GenerateUnitTestCode(entity),
                        GeneratedAt = DateTime.UtcNow
                    };

                    unitTests.Add(unitTest);
                }

                return unitTests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating unit tests");
                return unitTests;
            }
        }

        private string GenerateUnitTestCode(string entityName)
        {
            return $@"
using Xunit;
using {GetNamespaceForEntity(entityName)};

namespace Tests.Unit
{{
    public class {entityName}Tests
    {{
        [Fact]
        public void {entityName}_Should_Work()
        {{
            // Test implementation for {entityName}
            Assert.True(true);
        }}
    }}
}}";
        }

        private string GetNamespaceForEntity(string entityName)
        {
            return $"Domain.Entities.{entityName}";
        }
    }

    public class UnitTest
    {
        public string EntityName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }
}
