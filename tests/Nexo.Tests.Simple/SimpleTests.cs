using System;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Tests.Simple
{
    public class SimpleTests
    {
        [Fact]
        public void TestBasicMath()
        {
            // Arrange
            var a = 5;
            var b = 3;

            // Act
            var result = a + b;

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public void TestStringOperations()
        {
            // Arrange
            var str1 = "Hello";
            var str2 = "World";

            // Act
            var result = $"{str1} {str2}";

            // Assert
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public async Task TestAsyncOperation()
        {
            // Arrange
            var delay = 10;

            // Act
            await Task.Delay(delay);

            // Assert
            Assert.True(true);
        }

        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(5, 5, 10)]
        [InlineData(-1, 1, 0)]
        public void TestAddition(int a, int b, int expected)
        {
            // Act
            var result = a + b;

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestNullHandling()
        {
            // Arrange
            string? nullString = null;

            // Act & Assert
            Assert.Null(nullString);
            Assert.NotNull("not null");
        }
    }
}