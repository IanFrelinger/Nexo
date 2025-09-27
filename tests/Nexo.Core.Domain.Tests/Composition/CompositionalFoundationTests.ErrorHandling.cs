using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Domain.Tests.Composition
{
    /// <summary>
    /// Error handling tests for compositional foundation
    /// </summary>
    public partial class CompositionalFoundationTests
    {
        [Fact]
        public void ValidationResult_Failure_ReturnsInvalidResult()
        {
            // Arrange
            var errorMessage = "Test error message";
            
            // Act
            var result = ValidationResult.Failure(errorMessage);
            
            // Assert
            Assert.False(result.IsValid);
            Assert.False(result.HasWarnings);
            Assert.Single(result.Errors);
            Assert.Empty(result.Warnings);
            Assert.Equal(1, result.TotalIssues);
            Assert.Equal(errorMessage, result.Errors[0].Message);
            Assert.Equal("Validation failed with 1 error(s).", result.Summary);
        }
        
        [Fact]
        public void ValidationResult_Merge_CombinesResultsCorrectly()
        {
            // Arrange
            var result1 = new ValidationResult();
            result1.AddError("Error 1");
            result1.AddWarning("Warning 1");
            
            var result2 = new ValidationResult();
            result2.AddError("Error 2");
            result2.AddWarning("Warning 2");
            
            // Act
            result1.Merge(result2);
            
            // Assert
            Assert.False(result1.IsValid);
            Assert.True(result1.HasWarnings);
            Assert.Equal(2, result1.Errors.Count);
            Assert.Equal(2, result1.Warnings.Count);
            Assert.Equal(4, result1.TotalIssues);
        }
        
        [Fact]
        public void ValidationResult_Combine_CombinesMultipleResults()
        {
            // Arrange
            var result1 = ValidationResult.Failure("Error 1");
            var result2 = ValidationResult.Failure("Error 2");
            var result3 = ValidationResult.Success();
            
            // Act
            var combined = ValidationResult.Combine(result1, result2, result3);
            
            // Assert
            Assert.False(combined.IsValid);
            Assert.Equal(2, combined.Errors.Count);
            Assert.Equal("Validation failed with 2 error(s).", combined.Summary);
        }
        
        [Fact]
        public void ValidationResult_Combine_WithNullResults_HandlesGracefully()
        {
            // Act
            var combined = ValidationResult.Combine(null!, ValidationResult.Success(), null!);
            
            // Assert
            Assert.True(combined.IsValid);
            Assert.Equal("Validation passed successfully.", combined.Summary);
        }
        
        [Fact]
        public void ValidationResult_Merge_WithNullResult_ThrowsException()
        {
            // Arrange
            var result = new ValidationResult();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => result.Merge(null!));
        }
        
        [Fact]
        public void ValidationError_WithNullMessage_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationError(null!));
        }
        
        [Fact]
        public void ValidationWarning_WithNullMessage_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationWarning(null!));
        }
        
        [Fact]
        public void ValidationRule_WithNullParameters_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationRule(null!, "description", ValidationType.Required, "expression", "error", ValidationSeverity.Error));
            Assert.Throws<ArgumentNullException>(() => new ValidationRule("name", null!, ValidationType.Required, "expression", "error", ValidationSeverity.Error));
            Assert.Throws<ArgumentNullException>(() => new ValidationRule("name", "description", ValidationType.Required, null!, "error", ValidationSeverity.Error));
            Assert.Throws<ArgumentNullException>(() => new ValidationRule("name", "description", ValidationType.Required, "expression", null!, ValidationSeverity.Error));
        }
    }
}
