using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Core.Application.Commands;

namespace Nexo.Tests.Core.Application
{
    public class CoreApplicationTests
    {
        [Fact]
        public async Task TestProjectCommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestProjectCommand();
            var input = new TestProjectInput
            {
                ProjectName = "Test Project",
                ProjectPath = "/tmp/test",
                IncludeValidation = true,
                IncludeConfiguration = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Test Project", result.Data.ProjectName);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
        }

        [Fact]
        public async Task TestAgentCommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestAgentCommand();
            var input = new TestAgentInput
            {
                AgentName = "Test Agent",
                AgentType = "CrossPlatform",
                IncludeInitialization = true,
                IncludeCapabilities = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Test Agent", result.Data.AgentName);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
        }

        [Fact]
        public async Task TestAssemblyCommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestAssemblyCommand();
            var input = new TestAssemblyInput
            {
                AssemblyPath = "/tmp/test.dll",
                IncludeAnalysis = true,
                IncludeDecompilation = true,
                IncludeSecurityScan = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("/tmp/test.dll", result.Data.AssemblyPath);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
        }

        [Fact]
        public async Task TestApplicationOrchestrator_ExecuteApplicationTestSuiteAsync_ShouldReturnValidResults()
        {
            // Arrange
            var orchestrator = new TestApplicationOrchestrator();
            var input = new TestApplicationOrchestrationInput
            {
                IncludeProjectTests = true,
                IncludeAgentTests = true,
                IncludeAssemblyTests = true,
                TestEnvironment = "Test"
            };

            // Act
            var result = await orchestrator.ExecuteApplicationTestSuiteAsync(input);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.PassedTests > 0);
            Assert.Equal(0, result.FailedTests);
            Assert.True(result.ExecutionTime.TotalMilliseconds > 0);
            Assert.NotNull(result.TestResults);
        }

        [Fact]
        public async Task TestApplicationOrchestrator_ExecuteApplicationTestSuiteAsync_WithPartialTests_ShouldReturnValidResults()
        {
            // Arrange
            var orchestrator = new TestApplicationOrchestrator();
            var input = new TestApplicationOrchestrationInput
            {
                IncludeProjectTests = true,
                IncludeAgentTests = false,
                IncludeAssemblyTests = true,
                TestEnvironment = "Development"
            };

            // Act
            var result = await orchestrator.ExecuteApplicationTestSuiteAsync(input);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.PassedTests > 0);
            Assert.Equal(0, result.FailedTests);
        }

        [Theory]
        [InlineData("Test Project 1", "/tmp/project1")]
        [InlineData("Test Project 2", "/tmp/project2")]
        [InlineData("Test Project 3", "/tmp/project3")]
        public async Task TestProjectCommand_ExecuteAsync_WithDifferentInputs_ShouldReturnSuccess(string projectName, string projectPath)
        {
            // Arrange
            var command = new TestProjectCommand();
            var input = new TestProjectInput
            {
                ProjectName = projectName,
                ProjectPath = projectPath,
                IncludeValidation = true,
                IncludeConfiguration = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(projectName, result.Data.ProjectName);
            Assert.True(result.Data.Success);
        }

        [Theory]
        [InlineData("Agent 1", "CrossPlatform")]
        [InlineData("Agent 2", "CodeGeneration")]
        [InlineData("Agent 3", "SecurityAnalysis")]
        public async Task TestAgentCommand_ExecuteAsync_WithDifferentInputs_ShouldReturnSuccess(string agentName, string agentType)
        {
            // Arrange
            var command = new TestAgentCommand();
            var input = new TestAgentInput
            {
                AgentName = agentName,
                AgentType = agentType,
                IncludeInitialization = true,
                IncludeCapabilities = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(agentName, result.Data.AgentName);
            Assert.True(result.Data.Success);
        }

        [Theory]
        [InlineData("/tmp/assembly1.dll")]
        [InlineData("/tmp/assembly2.dll")]
        [InlineData("/tmp/assembly3.dll")]
        public async Task TestAssemblyCommand_ExecuteAsync_WithDifferentInputs_ShouldReturnSuccess(string assemblyPath)
        {
            // Arrange
            var command = new TestAssemblyCommand();
            var input = new TestAssemblyInput
            {
                AssemblyPath = assemblyPath,
                IncludeAnalysis = true,
                IncludeDecompilation = true,
                IncludeSecurityScan = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(assemblyPath, result.Data.AssemblyPath);
            Assert.True(result.Data.Success);
        }
    }
}
