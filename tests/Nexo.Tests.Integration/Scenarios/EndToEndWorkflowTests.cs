using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Integration.TestFixtures;
using Nexo.Tests.Integration.Commands;

namespace Nexo.Tests.Integration.Scenarios
{
    /// <summary>
    /// End-to-end workflow integration tests
    /// </summary>
    public class EndToEndWorkflowTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public EndToEndWorkflowTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CompleteWorkflow_ProjectCreationToAgentExecution_ShouldSucceed()
        {
            // Arrange & Act
            var result = await _fixture.ExecuteCompleteWorkflowAsync();

            // Assert
            Assert.True(result.Success);
            Assert.True(result.ProjectCreated);
            Assert.True(result.AgentCreated);
            Assert.True(result.AssemblyAnalyzed);
            Assert.True(result.ExecutionTime.TotalMilliseconds > 0);
            Assert.NotEmpty(result.StepResults);
        }

        [Fact]
        public async Task ProjectCreation_WithValidInput_ShouldSucceed()
        {
            // Act
            var result = await _fixture.CreateTestProjectAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Integration Test Project", result.Data.Name);
            Assert.Equal(_fixture.TestProjectPath, result.Data.Path);
        }

        [Fact]
        public async Task AgentCreation_WithValidInput_ShouldSucceed()
        {
            // Act
            var result = await _fixture.CreateTestAgentAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(_fixture.TestAgentName, result.Data.Name);
            Assert.Equal("Developer", result.Data.Role);
        }

        [Fact]
        public async Task AssemblyAnalysis_WithValidInput_ShouldSucceed()
        {
            // Act
            var result = await _fixture.AnalyzeTestAssemblyAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(_fixture.TestAssemblyPath, result.Data.AssemblyPath);
        }

        [Theory]
        [InlineData("Full", true, true, true)]
        [InlineData("ProjectOnly", true, false, false)]
        [InlineData("AgentOnly", false, true, false)]
        [InlineData("AssemblyOnly", false, false, true)]
        public async Task IntegrationCommand_WithDifferentScenarios_ShouldExecuteCorrectly(
            string testType, 
            bool includeProject, 
            bool includeAgent, 
            bool includeAssembly)
        {
            // Arrange
            var command = new TestIntegrationCommand();
            var input = new TestIntegrationInput
            {
                IntegrationTestType = testType,
                IncludeProjectTests = includeProject,
                IncludeAgentTests = includeAgent,
                IncludeAssemblyTests = includeAssembly,
                TestEnvironment = "Integration"
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(testType, result.Data.IntegrationTestType);
            Assert.True(result.Data.Success);
        }

        [Fact]
        public async Task MasterOrchestrator_FullTestSuite_ShouldExecuteAllSuites()
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
                TestEnvironment = "Integration",
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
    }
}
