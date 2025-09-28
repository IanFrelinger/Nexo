using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.CLI.Commands;

namespace Nexo.Tests.CLI
{
    public class CLITests : IDisposable
    {
        private readonly TestCLICommand _command;
        private readonly TestCLIOrchestrator _orchestrator;

        public CLITests()
        {
            _command = new TestCLICommand();
            _orchestrator = new TestCLIOrchestrator();
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var input = new TestCLIInput
            {
                TestName = "Basic CLI Test",
                Arguments = new[] { "--version" },
                Verbose = true,
                TimeoutMs = 5000,
                EnableRetries = true
            };

            // Act
            var result = await _command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Basic CLI Test", result.Data.TestName);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
            Assert.True(result.Data.ExecutionTime.TotalMilliseconds > 0);
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_WithNullInput_ShouldReturnFailure()
        {
            // Act
            var result = await _command.ExecuteAsync(null!);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Input cannot be null", result.ErrorMessage);
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_WithEmptyTestName_ShouldReturnFailure()
        {
            // Arrange
            var input = new TestCLIInput
            {
                TestName = "",
                Arguments = new[] { "--version" }
            };

            // Act
            var result = await _command.ExecuteAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Test name cannot be null or empty", result.ErrorMessage);
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_WithInvalidTimeout_ShouldReturnFailure()
        {
            // Arrange
            var input = new TestCLIInput
            {
                TestName = "Timeout Test",
                Arguments = new[] { "--version" },
                TimeoutMs = -1
            };

            // Act
            var result = await _command.ExecuteAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Input validation failed", result.ErrorMessage);
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_WithExcessiveTimeout_ShouldReturnFailure()
        {
            // Arrange
            var input = new TestCLIInput
            {
                TestName = "Excessive Timeout Test",
                Arguments = new[] { "--version" },
                TimeoutMs = 60000
            };

            // Act
            var result = await _command.ExecuteAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Input validation failed", result.ErrorMessage);
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_WithEmptyInput_ShouldReturnSuccess()
        {
            // Arrange
            var input = new TestCLIInput
            {
                TestName = "Empty Input Test",
                Arguments = Array.Empty<string>(),
                Verbose = false,
                TimeoutMs = 5000,
                EnableRetries = true
            };

            // Act
            var result = await _command.ExecuteAsync(input);

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
            var input = new TestCLIOrchestrationInput
            {
                TestArguments = new[] { "--test", "--verbose" },
                IncludeVerboseTests = true,
                TestEnvironment = "Test",
                TimeoutMs = 30000,
                TestTimeoutMs = 5000,
                EnableRetries = true
            };

            // Act
            var result = await _orchestrator.ExecuteCLITestSuiteAsync(input);

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
            // Act
            var result = await _orchestrator.ExecuteCLITestSuiteAsync(null!);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Input cannot be null", result.ErrorMessage);
        }

        [Fact]
        public async Task TestCLIOrchestrator_ExecuteCLITestSuiteAsync_WithInvalidTimeouts_ShouldReturnFailure()
        {
            // Arrange
            var input = new TestCLIOrchestrationInput
            {
                TestArguments = new[] { "--test" },
                TimeoutMs = -1,
                TestTimeoutMs = -1
            };

            // Act
            var result = await _orchestrator.ExecuteCLITestSuiteAsync(input);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Orchestration input validation failed", result.ErrorMessage);
        }

        [Fact]
        public async Task TestCLIOrchestrator_ExecuteCLITestSuiteAsync_WithEmptyArguments_ShouldStillSucceed()
        {
            // Arrange
            var input = new TestCLIOrchestrationInput
            {
                TestArguments = Array.Empty<string>(),
                IncludeVerboseTests = false,
                TestEnvironment = "Development",
                TimeoutMs = 30000,
                TestTimeoutMs = 5000,
                EnableRetries = true
            };

            // Act
            var result = await _orchestrator.ExecuteCLITestSuiteAsync(input);

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
            var input = new TestCLIInput
            {
                TestName = $"Test with {argument}",
                Arguments = new[] { argument },
                Verbose = true,
                TimeoutMs = 5000,
                EnableRetries = true
            };

            // Act
            var result = await _command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal($"Test with {argument}", result.Data.TestName);
            Assert.True(result.Data.Success);
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_WithRetryDisabled_ShouldStillWork()
        {
            // Arrange
            var input = new TestCLIInput
            {
                TestName = "Retry Disabled Test",
                Arguments = new[] { "--version" },
                EnableRetries = false,
                TimeoutMs = 5000
            };

            // Act
            var result = await _command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.Success);
        }

        [Fact]
        public async Task TestCLICommand_ExecuteAsync_WithManyArguments_ShouldReturnWarning()
        {
            // Arrange
            var input = new TestCLIInput
            {
                TestName = "Many Arguments Test",
                Arguments = new[] { "--arg1", "--arg2", "--arg3", "--arg4", "--arg5", "--arg6", "--arg7", "--arg8", "--arg9", "--arg10", "--arg11" },
                TimeoutMs = 5000
            };

            // Act
            var result = await _command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.Warnings.Length > 0);
        }

        public void Dispose()
        {
            _command?.Dispose();
            _orchestrator?.Dispose();
        }
    }
}
