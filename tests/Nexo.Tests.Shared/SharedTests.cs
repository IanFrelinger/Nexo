using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Shared.Commands;

namespace Nexo.Tests.Shared
{
    public class SharedTests
    {
        [Fact]
        public async Task TestSharedCommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestSharedCommand();
            var input = new TestSharedInput
            {
                SharedComponentType = "All",
                IncludeResourceTests = true,
                IncludePlatformTests = true,
                IncludeModelTests = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("All", result.Data.SharedComponentType);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
        }

        [Fact]
        public async Task TestSharedCommand_ExecuteAsync_WithPartialTests_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestSharedCommand();
            var input = new TestSharedInput
            {
                SharedComponentType = "Platform",
                IncludeResourceTests = false,
                IncludePlatformTests = true,
                IncludeModelTests = false
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Platform", result.Data.SharedComponentType);
            Assert.True(result.Data.Success);
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
            var command = new TestSharedCommand();
            var input = new TestSharedInput
            {
                SharedComponentType = componentType,
                IncludeResourceTests = includeResource,
                IncludePlatformTests = includePlatform,
                IncludeModelTests = includeModel
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(componentType, result.Data.SharedComponentType);
            Assert.True(result.Data.Success);
        }
    }
}
