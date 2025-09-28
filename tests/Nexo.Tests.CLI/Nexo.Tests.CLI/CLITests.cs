using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.CLI.Commands;

namespace Nexo.Tests.CLI
{
    public class CLITests
    {
        [Fact]
        public async Task TestCLICommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestCLICommand();
            var input = new TestCLIInput
            {
                TestName = "Basic CLI Test",
                Arguments = new[] { "--version" },
                Verbose = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Basic CLI Test", result.Data.TestName);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_WithNullInput_ShouldReturnFailure()
        {
            // Arrange
            var command = new TestCLICommand();

            // Act
            var result = await command.ExecuteAsync(null!);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Input cannot be null", result.ErrorMessage);
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_WithEmptyTestName_ShouldReturnFailure()
        {
            // Arrange
            var command = new TestCLICommand();
            var input = new TestCLIInput
            {
                TestName = "",
                Arguments = new[] { "--version" }
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Test name cannot be null or empty", result.ErrorMessage);
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_WithEmptyInput_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestCLICommand();
            var input = new TestCLIInput
            {
                TestName = "Empty Input Test",
                Arguments = Array.Empty<string>(),
                Verbose = false
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Empty Input Test", result.Data.TestName);
            Assert.True(result.Data.Success);
        }

        [Fact]
        public async Task TestCLIOrchestrator_ExecuteCLITestSuiteAsync_ShouldReturnValidResults()
        {
            // Arrange
            var orchestrator = new TestCLIOrchestrator();
            var input = new TestCLIOrchestrationInput
            {
                TestArguments = new[] { "--test", "--verbose" },
                IncludeVerboseTests = true,
                TestEnvironment = "Test"
            };

            // Act
            var result = await orchestrator.ExecuteCLITestSuiteAsync(input);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.PassedTests > 0);
            Assert.Equal(0, result.FailedTests);
            Assert.True(result.ExecutionTime.TotalMilliseconds > 0);
            Assert.NotNull(result.TestResults);
        }

        [Fact]
        public async Task TestCLIOrchestrator_ExecuteCLITestSuiteAsync_WithNullInput_ShouldReturnFailure()
        {
            // Arrange
            var orchestrator = new TestCLIOrchestrator();

            // Act
            var result = await orchestrator.ExecuteCLITestSuiteAsync(null!);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Input cannot be null", result.ErrorMessage);
        }

        [Fact]
        public async Task TestCLIOrchestrator_ExecuteCLITestSuiteAsync_WithEmptyArguments_ShouldStillSucceed()
        {
            // Arrange
            var orchestrator = new TestCLIOrchestrator();
            var input = new TestCLIOrchestrationInput
            {
                TestArguments = Array.Empty<string>(),
                IncludeVerboseTests = false,
                TestEnvironment = "Development"
            };

            // Act
            var result = await orchestrator.ExecuteCLITestSuiteAsync(input);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.PassedTests > 0);
            Assert.Equal(0, result.FailedTests);
        }

        [Theory]
        [InlineData("--version")]
        [InlineData("--help")]
        [InlineData("--test")]
        [InlineData("--verbose")]
        public async Task TestCLICommand_ExecuteAsync_WithDifferentArguments_ShouldReturnSuccess(string argument)
        {
            // Arrange
            var command = new TestCLICommand();
            var input = new TestCLIInput
            {
                TestName = $"Test with {argument}",
                Arguments = new[] { argument },
                Verbose = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal($"Test with {argument}", result.Data.TestName);
            Assert.True(result.Data.Success);
        }
    }
}
