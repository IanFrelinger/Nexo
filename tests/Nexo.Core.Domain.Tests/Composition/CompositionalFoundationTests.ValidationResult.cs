using System;
using Xunit;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Domain.Tests.Composition
{
    /// <summary>
    /// Validation result tests for compositional foundation.
    /// </summary>
    public partial class CompositionalFoundationTests
    {
        [Fact]
        public void ValidationResult_Success_ReturnsValidResult()
        {
            // Act
            var result = ValidationResult.Success();
            
            // Assert
            Assert.True(result.IsValid);
            Assert.False(result.HasWarnings);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Warnings);
            Assert.Equal(0, result.TotalIssues);
            Assert.Equal("Validation passed successfully.", result.Summary);
        }
        
        [Fact]
        public void ValidationResult_AddError_AddsErrorCorrectly()
        {
            // Arrange
            var result = new ValidationResult();
            var errorMessage = "Test error";
            var property = "TestProperty";
            var code = "TEST_ERROR";
            
            // Act
            result.AddError(errorMessage, property, code);
            
            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            var error = result.Errors[0];
            Assert.Equal(errorMessage, error.Message);
            Assert.Equal(property, error.Property);
            Assert.Equal(code, error.Code);
        }
        
        [Fact]
        public void ValidationResult_AddWarning_AddsWarningCorrectly()
        {
            // Arrange
            var result = new ValidationResult();
            var warningMessage = "Test warning";
            var property = "TestProperty";
            var code = "TEST_WARNING";
            
            // Act
            result.AddWarning(warningMessage, property, code);
            
            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.HasWarnings);
            Assert.Empty(result.Errors);
            Assert.Single(result.Warnings);
            var warning = result.Warnings[0];
            Assert.Equal(warningMessage, warning.Message);
            Assert.Equal(property, warning.Property);
            Assert.Equal(code, warning.Code);
        }
        
        [Fact]
        public void ValidationResult_ToString_ReturnsFormattedString()
        {
            // Arrange
            var result = new ValidationResult();
            result.AddError("Error 1", "Property1");
            result.AddWarning("Warning 1", "Property2");
            
            // Act
            var resultString = result.ToString();
            
            // Assert
            Assert.Contains("Validation failed with 1 error(s) and 1 warning(s).", resultString);
            Assert.Contains("Errors:", resultString);
            Assert.Contains("Warnings:", resultString);
            Assert.Contains("Error 1", resultString);
            Assert.Contains("Warning 1", resultString);
        }
    }
}
