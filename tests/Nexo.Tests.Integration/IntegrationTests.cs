using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Integration.Commands;

namespace Nexo.Tests.Integration
{
    public class IntegrationTests
    {
        [Fact]
        public async Task TestIntegrationCommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestIntegrationCommand();
            var input = new TestIntegrationInput
            {
                IntegrationTestType = "Full",
                IncludeProjectTests = true,
                IncludeAgentTests = true,
                IncludeAssemblyTests = true,
                TestEnvironment = "Development"
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Full", result.Data.IntegrationTestType);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
        }

        [Fact]
        public async Task TestIntegrationCommand_ExecuteAsync_WithPartialTests_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestIntegrationCommand();
            var input = new TestIntegrationInput
            {
                IntegrationTestType = "Partial",
                IncludeProjectTests = true,
                IncludeAgentTests = false,
                IncludeAssemblyTests = true,
                TestEnvironment = "Test"
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Partial", result.Data.IntegrationTestType);
            Assert.True(result.Data.Success);
        }

        [Theory]
        [InlineData("Full", true, true, true)]
        [InlineData("ProjectOnly", true, false, false)]
        [InlineData("AgentOnly", false, true, false)]
        [InlineData("AssemblyOnly", false, false, true)]
        public async Task TestIntegrationCommand_ExecuteAsync_WithDifferentInputs_ShouldReturnSuccess(
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
                TestEnvironment = "Development"
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(testType, result.Data.IntegrationTestType);
            Assert.True(result.Data.Success);
        }
    }
}
