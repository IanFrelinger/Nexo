using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Core.Domain.Commands;

namespace Nexo.Tests.Core.Domain
{
    public class CoreDomainTests
    {
        [Fact]
        public async Task TestAgentDomainCommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestAgentDomainCommand();
            var input = new TestAgentDomainInput
            {
                AgentName = "Test Agent",
                AgentRole = "Developer",
                Platform = "Windows",
                IncludeCapabilities = true,
                IncludeStatus = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Test Agent", result.Data.AgentName);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
            Assert.NotEmpty(result.Data.AgentId);
        }

        [Fact]
        public async Task TestValueObjectCommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestValueObjectCommand();
            var input = new TestValueObjectInput
            {
                ValueObjectType = "All",
                AgentName = "Test Agent",
                IncludeEqualityTests = true,
                IncludeCreationTests = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("All", result.Data.ValueObjectType);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
        }

        [Fact]
        public async Task TestDomainOrchestrator_ExecuteDomainTestSuiteAsync_ShouldReturnValidResults()
        {
            // Arrange
            var orchestrator = new TestDomainOrchestrator();
            var input = new TestDomainOrchestrationInput
            {
                IncludeAgentTests = true,
                IncludeValueObjectTests = true,
                TestEnvironment = "Test"
            };

            // Act
            var result = await orchestrator.ExecuteDomainTestSuiteAsync(input);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.PassedTests > 0);
            Assert.Equal(0, result.FailedTests);
            Assert.True(result.ExecutionTime.TotalMilliseconds > 0);
            Assert.NotNull(result.TestResults);
        }

        [Fact]
        public async Task TestDomainOrchestrator_ExecuteDomainTestSuiteAsync_WithPartialTests_ShouldReturnValidResults()
        {
            // Arrange
            var orchestrator = new TestDomainOrchestrator();
            var input = new TestDomainOrchestrationInput
            {
                IncludeAgentTests = true,
                IncludeValueObjectTests = false,
                TestEnvironment = "Development"
            };

            // Act
            var result = await orchestrator.ExecuteDomainTestSuiteAsync(input);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.PassedTests > 0);
            Assert.Equal(0, result.FailedTests);
        }

        [Theory]
        [InlineData("Agent 1", "Developer", "Windows")]
        [InlineData("Agent 2", "Analyzer", "macOS")]
        [InlineData("Agent 3", "Security", "Linux")]
        public async Task TestAgentDomainCommand_ExecuteAsync_WithDifferentInputs_ShouldReturnSuccess(string agentName, string agentRole, string platform)
        {
            // Arrange
            var command = new TestAgentDomainCommand();
            var input = new TestAgentDomainInput
            {
                AgentName = agentName,
                AgentRole = agentRole,
                Platform = platform,
                IncludeCapabilities = true,
                IncludeStatus = true
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
        [InlineData("AgentId", "Test Agent 1")]
        [InlineData("AgentName", "Test Agent 2")]
        [InlineData("All", "Test Agent 3")]
        public async Task TestValueObjectCommand_ExecuteAsync_WithDifferentInputs_ShouldReturnSuccess(string valueObjectType, string agentName)
        {
            // Arrange
            var command = new TestValueObjectCommand();
            var input = new TestValueObjectInput
            {
                ValueObjectType = valueObjectType,
                AgentName = agentName,
                IncludeEqualityTests = true,
                IncludeCreationTests = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(valueObjectType, result.Data.ValueObjectType);
            Assert.True(result.Data.Success);
        }
    }
}
