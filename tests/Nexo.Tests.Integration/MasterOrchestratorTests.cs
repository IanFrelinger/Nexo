using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Integration.Commands;

namespace Nexo.Tests.Integration
{
    public class MasterOrchestratorTests
    {
        [Fact]
        public async Task TestMasterOrchestrator_ExecuteFullTestSuiteAsync_ShouldReturnValidResults()
        {
            // Arrange
            var orchestrator = new TestMasterOrchestrator();
            var input = new TestMasterOrchestrationInput
            {
                IncludeCLITests = true,
                IncludeCoreApplicationTests = true,
                IncludeCoreDomainTests = true,
                IncludeSharedTests = true,
                IncludeIntegrationTests = true,
                TestEnvironment = "Test",
                GenerateCoverageReport = true
            };

            // Act
            var result = await orchestrator.ExecuteFullTestSuiteAsync(input);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.TotalTestSuites > 0);
            Assert.True(result.PassedTestSuites > 0);
            Assert.Equal(0, result.FailedTestSuites);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.TotalPassedTests > 0);
            Assert.Equal(0, result.TotalFailedTests);
            Assert.True(result.ExecutionTime.TotalMilliseconds > 0);
            Assert.NotNull(result.TestSuiteResults);
        }

        [Fact]
        public async Task TestMasterOrchestrator_ExecuteFullTestSuiteAsync_WithPartialTests_ShouldReturnValidResults()
        {
            // Arrange
            var orchestrator = new TestMasterOrchestrator();
            var input = new TestMasterOrchestrationInput
            {
                IncludeCLITests = true,
                IncludeCoreApplicationTests = false,
                IncludeCoreDomainTests = true,
                IncludeSharedTests = false,
                IncludeIntegrationTests = true,
                TestEnvironment = "Development",
                GenerateCoverageReport = false
            };

            // Act
            var result = await orchestrator.ExecuteFullTestSuiteAsync(input);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.TotalTestSuites > 0);
            Assert.True(result.PassedTestSuites > 0);
            Assert.Equal(0, result.FailedTestSuites);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.TotalPassedTests > 0);
            Assert.Equal(0, result.TotalFailedTests);
        }

        [Theory]
        [InlineData(true, true, true, true, true)]
        [InlineData(true, false, false, false, false)]
        [InlineData(false, true, false, false, false)]
        [InlineData(false, false, true, false, false)]
        [InlineData(false, false, false, true, false)]
        [InlineData(false, false, false, false, true)]
        public async Task TestMasterOrchestrator_ExecuteFullTestSuiteAsync_WithDifferentTestCombinations_ShouldReturnValidResults(
            bool includeCLI,
            bool includeCoreApplication,
            bool includeCoreDomain,
            bool includeShared,
            bool includeIntegration)
        {
            // Arrange
            var orchestrator = new TestMasterOrchestrator();
            var input = new TestMasterOrchestrationInput
            {
                IncludeCLITests = includeCLI,
                IncludeCoreApplicationTests = includeCoreApplication,
                IncludeCoreDomainTests = includeCoreDomain,
                IncludeSharedTests = includeShared,
                IncludeIntegrationTests = includeIntegration,
                TestEnvironment = "Test",
                GenerateCoverageReport = true
            };

            // Act
            var result = await orchestrator.ExecuteFullTestSuiteAsync(input);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.TotalTestSuites > 0);
            Assert.True(result.PassedTestSuites > 0);
            Assert.Equal(0, result.FailedTestSuites);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.TotalPassedTests > 0);
            Assert.Equal(0, result.TotalFailedTests);
        }

        [Fact]
        public async Task TestMasterOrchestrator_ExecuteFullTestSuiteAsync_WithAllTestsDisabled_ShouldReturnEmptyResults()
        {
            // Arrange
            var orchestrator = new TestMasterOrchestrator();
            var input = new TestMasterOrchestrationInput
            {
                IncludeCLITests = false,
                IncludeCoreApplicationTests = false,
                IncludeCoreDomainTests = false,
                IncludeSharedTests = false,
                IncludeIntegrationTests = false,
                TestEnvironment = "Test",
                GenerateCoverageReport = false
            };

            // Act
            var result = await orchestrator.ExecuteFullTestSuiteAsync(input);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.TotalTestSuites);
            Assert.Equal(0, result.PassedTestSuites);
            Assert.Equal(0, result.FailedTestSuites);
            Assert.Equal(0, result.TotalTests);
            Assert.Equal(0, result.TotalPassedTests);
            Assert.Equal(0, result.TotalFailedTests);
        }
    }
}
