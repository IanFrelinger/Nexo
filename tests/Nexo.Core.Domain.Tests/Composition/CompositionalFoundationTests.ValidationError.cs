using System;
using Xunit;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Domain.Tests.Composition
{
    /// <summary>
    /// Validation error tests for compositional foundation.
    /// </summary>
    public partial class CompositionalFoundationTests
    {
        [Fact]
        public void ValidationError_Creation_SetsPropertiesCorrectly()
        {
            // Arrange
            var message = "Test error";
            var property = "TestProperty";
            var code = "TEST_ERROR";
            
            // Act
            var error = new ValidationError(message, property, code);
            
            // Assert
            Assert.Equal(message, error.Message);
            Assert.Equal(property, error.Property);
            Assert.Equal(code, error.Code);
            Assert.True(error.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
        }
        
        [Fact]
        public void ValidationError_ForProperty_CreatesPropertySpecificError()
        {
            // Arrange
            var property = "TestProperty";
            var message = "Test error";
            var code = "TEST_ERROR";
            
            // Act
            var error = ValidationError.ForProperty(property, message, code);
            
            // Assert
            Assert.Equal(message, error.Message);
            Assert.Equal(property, error.Property);
            Assert.Equal(code, error.Code);
        }
        
        [Fact]
        public void ValidationError_ToString_ReturnsFormattedString()
        {
            // Arrange
            var error = new ValidationError("Test error", "TestProperty", "TEST_ERROR");
            
            // Act
            var errorString = error.ToString();
            
            // Assert
            Assert.Contains("Property: TestProperty", errorString);
            Assert.Contains("Code: TEST_ERROR", errorString);
            Assert.Contains("Message: Test error", errorString);
        }
        
        [Fact]
        public void ValidationError_Equals_ComparesCorrectly()
        {
            // Arrange
            var error1 = new ValidationError("Test error", "Property1", "CODE1");
            var error2 = new ValidationError("Test error", "Property1", "CODE1");
            var error3 = new ValidationError("Different error", "Property1", "CODE1");
            
            // Act & Assert
            Assert.Equal(error1, error2);
            Assert.NotEqual(error1, error3);
            Assert.NotEqual(null!, error1);
        }
    }
}
