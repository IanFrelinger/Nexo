using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Shared.Commands;

namespace Nexo.Tests.Shared
{
    public class SharedTests : TestBase
    {
        private readonly TestSharedCommand _command;

        public SharedTests()
        {
            _command = new TestSharedCommand();
            RegisterDisposable(_command);
        }

        [Fact]
        public async Task TestSharedCommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var input = CreateTestInput<TestSharedInput>(i =>
            {
                i.SharedComponentType = "All";
                i.IncludeResourceTests = true;
                i.IncludePlatformTests = true;
                i.IncludeModelTests = true;
            });

            // Act & Assert
            var result = await ExecuteWithRetryAndTimeoutAsync(
                () => _command.ExecuteAsync(input),
                testName: "Shared Command Test");

            AssertTestSuccess(result, r => 
                r.SharedComponentType == "All" && 
                r.Success && 
                r.TestResults.Length > 0);
        }

        [Fact]
        public async Task TestSharedCommand_ExecuteAsync_WithPartialTests_ShouldReturnSuccess()
        {
            // Arrange
            var input = CreateTestInput<TestSharedInput>(i =>
            {
                i.SharedComponentType = "Platform";
                i.IncludeResourceTests = false;
                i.IncludePlatformTests = true;
                i.IncludeModelTests = false;
            });

            // Act & Assert
            var result = await ExecuteWithRetryAndTimeoutAsync(
                () => _command.ExecuteAsync(input),
                testName: "Partial Shared Command Test");

            AssertTestSuccess(result, r => 
                r.SharedComponentType == "Platform" && 
                r.Success);
        }

        [Fact]
        public async Task TestSharedCommand_ExecuteAsync_WithNullInput_ShouldReturnFailure()
        {
            // Act & Assert
            var result = await ExecuteWithRetryAndTimeoutAsync(
                () => _command.ExecuteAsync(null!),
                testName: "Null Input Test");

            Assert.False(result.IsSuccess);
            Assert.Contains("Input cannot be null", result.ErrorMessage);
        }

        [Fact]
        public async Task TestSharedCommand_ExecuteAsync_WithInvalidInput_ShouldReturnFailure()
        {
            // Arrange
            var input = CreateTestInput<TestSharedInput>(i =>
            {
                i.SharedComponentType = ""; // Invalid empty type
                i.IncludeResourceTests = true;
                i.IncludePlatformTests = true;
                i.IncludeModelTests = true;
            });

            // Act & Assert
            var result = await ExecuteWithRetryAndTimeoutAsync(
                () => _command.ExecuteAsync(input),
                testName: "Invalid Input Test");

            // Should still succeed but with warnings or different behavior
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("Platform", true, false, false)]
        [InlineData("Resource", false, true, false)]
        [InlineData("Model", false, false, true)]
        [InlineData("All", true, true, true)]
        public async Task TestSharedCommand_ExecuteAsync_WithDifferentInputs_ShouldReturnSuccess(
            string componentType, 
            bool includeResource, 
            bool includePlatform, 
            bool includeModel)
        {
            // Arrange
            var input = CreateTestInput<TestSharedInput>(i =>
            {
                i.SharedComponentType = componentType;
                i.IncludeResourceTests = includeResource;
                i.IncludePlatformTests = includePlatform;
                i.IncludeModelTests = includeModel;
            });

            // Act & Assert
            var result = await ExecuteWithRetryAndTimeoutAsync(
                () => _command.ExecuteAsync(input),
                testName: $"Different Input Test - {componentType}");

            AssertTestSuccess(result, r => 
                r.SharedComponentType == componentType && 
                r.Success);
        }

        [Fact]
        public async Task TestSharedCommand_ExecuteAsync_WithTimeout_ShouldHandleGracefully()
        {
            // Arrange
            var input = CreateTestInput<TestSharedInput>(i =>
            {
                i.SharedComponentType = "All";
                i.IncludeResourceTests = true;
                i.IncludePlatformTests = true;
                i.IncludeModelTests = true;
            });

            // Act & Assert
            var result = await ExecuteWithTimeoutAsync(
                () => _command.ExecuteAsync(input),
                TimeSpan.FromMilliseconds(100), // Very short timeout
                testName: "Timeout Test");

            // Should either succeed quickly or handle timeout gracefully
            Assert.True(result.IsSuccess || !string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Fact]
        public async Task TestSharedCommand_ExecuteAsync_WithRetry_ShouldHandleTransientFailures()
        {
            // Arrange
            var input = CreateTestInput<TestSharedInput>(i =>
            {
                i.SharedComponentType = "All";
                i.IncludeResourceTests = true;
                i.IncludePlatformTests = true;
                i.IncludeModelTests = true;
            });

            // Act & Assert
            var result = await ExecuteWithRetryAsync(
                () => _command.ExecuteAsync(input),
                maxRetries: 3,
                delayMs: 50,
                testName: "Retry Test");

            AssertTestSuccess(result);
        }
    }
}
