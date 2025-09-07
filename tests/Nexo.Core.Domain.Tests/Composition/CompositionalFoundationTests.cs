using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Domain.Tests.Composition
{
    /// <summary>
    /// Tests for the compositional foundation including interfaces, base classes, and validation framework.
    /// </summary>
    public class CompositionalFoundationTests
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
        public void ValidationWarning_Creation_SetsPropertiesCorrectly()
        {
            // Arrange
            var message = "Test warning";
            var property = "TestProperty";
            var code = "TEST_WARNING";
            var severity = WarningSeverity.High;
            
            // Act
            var warning = new ValidationWarning(message, property, code, severity);
            
            // Assert
            Assert.Equal(message, warning.Message);
            Assert.Equal(property, warning.Property);
            Assert.Equal(code, warning.Code);
            Assert.Equal(severity, warning.Severity);
            Assert.True(warning.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
        }
        
        [Fact]
        public void ValidationWarning_StaticFactories_CreateCorrectWarnings()
        {
            // Act & Assert
            var highWarning = ValidationWarning.High("High warning");
            Assert.Equal(WarningSeverity.High, highWarning.Severity);
            
            var mediumWarning = ValidationWarning.Medium("Medium warning");
            Assert.Equal(WarningSeverity.Medium, mediumWarning.Severity);
            
            var lowWarning = ValidationWarning.Low("Low warning");
            Assert.Equal(WarningSeverity.Low, lowWarning.Severity);
        }
        
        [Fact]
        public void ValidationRule_Creation_SetsPropertiesCorrectly()
        {
            // Arrange
            var name = "TestRule";
            var description = "Test description";
            var type = ValidationType.Required;
            var expression = "!string.IsNullOrEmpty(value)";
            var errorMessage = "Value is required";
            var severity = ValidationSeverity.Error;
            
            // Act
            var rule = new ValidationRule(name, description, type, expression, errorMessage, severity);
            
            // Assert
            Assert.Equal(name, rule.Name);
            Assert.Equal(description, rule.Description);
            Assert.Equal(type, rule.Type);
            Assert.Equal(expression, rule.Expression);
            Assert.Equal(errorMessage, rule.ErrorMessage);
            Assert.Equal(severity, rule.Severity);
        }
        
        [Fact]
        public void ValidationRule_Create_WithValidationFunction()
        {
            // Arrange
            var name = "TestRule";
            var description = "Test description";
            var errorMessage = "Value is invalid";
            Func<object, bool> validationFunc = obj => obj?.ToString()?.Length > 0;
            
            // Act
            var rule = ValidationRule.Create(name, description, validationFunc, errorMessage);
            
            // Assert
            Assert.Equal(name, rule.Name);
            Assert.Equal(description, rule.Description);
            Assert.Equal(ValidationType.Custom, rule.Type);
            Assert.Equal(errorMessage, rule.ErrorMessage);
            Assert.True(rule.Validate("test"));
            Assert.False(rule.Validate(""));
        }
        
        [Fact]
        public void ValidationRule_Required_CreatesRequiredRule()
        {
            // Arrange
            var propertyName = "TestProperty";
            
            // Act
            var rule = ValidationRule.Required(propertyName);
            
            // Assert
            Assert.Equal($"Required_{propertyName}", rule.Name);
            Assert.Equal(ValidationType.Required, rule.Type);
            Assert.Equal(ValidationSeverity.Error, rule.Severity);
            Assert.True(rule.Validate("test"));
            Assert.False(rule.Validate(""));
            Assert.False(rule.Validate(null));
        }
        
        [Fact]
        public void ValidationRule_MinLength_CreatesMinLengthRule()
        {
            // Arrange
            var propertyName = "TestProperty";
            var minLength = 3;
            
            // Act
            var rule = ValidationRule.MinLength(propertyName, minLength);
            
            // Assert
            Assert.Equal($"MinLength_{propertyName}_{minLength}", rule.Name);
            Assert.Equal(ValidationType.Length, rule.Type);
            Assert.True(rule.Validate("test"));
            Assert.False(rule.Validate("ab"));
        }
        
        [Fact]
        public void ValidationRule_MaxLength_CreatesMaxLengthRule()
        {
            // Arrange
            var propertyName = "TestProperty";
            var maxLength = 5;
            
            // Act
            var rule = ValidationRule.MaxLength(propertyName, maxLength);
            
            // Assert
            Assert.Equal($"MaxLength_{propertyName}_{maxLength}", rule.Name);
            Assert.Equal(ValidationType.Length, rule.Type);
            Assert.True(rule.Validate("test"));
            Assert.False(rule.Validate("toolong"));
        }
        
        [Fact]
        public void ValidationRule_Pattern_CreatesPatternRule()
        {
            // Arrange
            var propertyName = "TestProperty";
            var pattern = @"^\d+$";
            var description = "Must be numeric";
            
            // Act
            var rule = ValidationRule.Pattern(propertyName, pattern, description);
            
            // Assert
            Assert.Equal($"Pattern_{propertyName}", rule.Name);
            Assert.Equal(ValidationType.Pattern, rule.Type);
            Assert.True(rule.Validate("123"));
            Assert.False(rule.Validate("abc"));
        }
        
        [Fact]
        public void ValidationRule_Compose_CreatesCompositeRule()
        {
            // Arrange
            var rule1 = ValidationRule.Required("Property1");
            var rule2 = ValidationRule.MinLength("Property2", 3);
            
            // Act
            var compositeRule = rule1.Compose(rule2);
            
            // Assert
            Assert.Equal(ValidationType.Composite, compositeRule.Type);
            Assert.Equal(2, compositeRule.ComposedRules.Count);
            Assert.Contains(rule1, compositeRule.ComposedRules);
            Assert.Contains(rule2, compositeRule.ComposedRules);
        }
        
        [Fact]
        public void ValidationRule_Decompose_ReturnsConstituentRules()
        {
            // Arrange
            var rule1 = ValidationRule.Required("Property1");
            var rule2 = ValidationRule.MinLength("Property2", 3);
            var compositeRule = rule1.Compose(rule2);
            
            // Act
            var decomposed = compositeRule.Decompose().ToList();
            
            // Assert
            Assert.Equal(2, decomposed.Count);
            Assert.Contains(rule1, decomposed);
            Assert.Contains(rule2, decomposed);
        }
        
        [Fact]
        public void ValidationRule_CanComposeWith_ReturnsTrueForValidRules()
        {
            // Arrange
            var rule1 = ValidationRule.Required("Property1");
            var rule2 = ValidationRule.MinLength("Property2", 3);
            
            // Act & Assert
            Assert.True(rule1.CanComposeWith(rule2));
            Assert.True(rule2.CanComposeWith(rule1));
            Assert.False(rule1.CanComposeWith(null));
        }
        
        [Fact]
        public void ValidationSeverity_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(ValidationSeverity), ValidationSeverity.Info));
            Assert.True(Enum.IsDefined(typeof(ValidationSeverity), ValidationSeverity.Warning));
            Assert.True(Enum.IsDefined(typeof(ValidationSeverity), ValidationSeverity.Error));
            Assert.True(Enum.IsDefined(typeof(ValidationSeverity), ValidationSeverity.Critical));
        }
        
        [Fact]
        public void ValidationType_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(ValidationType), ValidationType.Required));
            Assert.True(Enum.IsDefined(typeof(ValidationType), ValidationType.Length));
            Assert.True(Enum.IsDefined(typeof(ValidationType), ValidationType.Pattern));
            Assert.True(Enum.IsDefined(typeof(ValidationType), ValidationType.Custom));
            Assert.True(Enum.IsDefined(typeof(ValidationType), ValidationType.Composite));
        }
        
        [Fact]
        public void WarningSeverity_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(WarningSeverity), WarningSeverity.Low));
            Assert.True(Enum.IsDefined(typeof(WarningSeverity), WarningSeverity.Medium));
            Assert.True(Enum.IsDefined(typeof(WarningSeverity), WarningSeverity.High));
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
        public void ValidationWarning_ToString_ReturnsFormattedString()
        {
            // Arrange
            var warning = new ValidationWarning("Test warning", "TestProperty", "TEST_WARNING", WarningSeverity.High);
            
            // Act
            var warningString = warning.ToString();
            
            // Assert
            Assert.Contains("Property: TestProperty", warningString);
            Assert.Contains("Code: TEST_WARNING", warningString);
            Assert.Contains("Severity: High", warningString);
            Assert.Contains("Message: Test warning", warningString);
        }
        
        [Fact]
        public void ValidationRule_ToString_ReturnsFormattedString()
        {
            // Arrange
            var rule = new ValidationRule("TestRule", "Test description", ValidationType.Required, "expression", "error message", ValidationSeverity.Error);
            
            // Act
            var ruleString = rule.ToString();
            
            // Assert
            Assert.Contains("Name: TestRule", ruleString);
            Assert.Contains("Type: Required", ruleString);
            Assert.Contains("Severity: Error", ruleString);
            Assert.Contains("Expression: expression", ruleString);
            Assert.Contains("Description: Test description", ruleString);
            Assert.Contains("Error: error message", ruleString);
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
            Assert.NotEqual(error1, null);
        }
        
        [Fact]
        public void ValidationWarning_Equals_ComparesCorrectly()
        {
            // Arrange
            var warning1 = new ValidationWarning("Test warning", "Property1", "CODE1", WarningSeverity.High);
            var warning2 = new ValidationWarning("Test warning", "Property1", "CODE1", WarningSeverity.High);
            var warning3 = new ValidationWarning("Test warning", "Property1", "CODE1", WarningSeverity.Low);
            
            // Act & Assert
            Assert.Equal(warning1, warning2);
            Assert.NotEqual(warning1, warning3);
            Assert.NotEqual(warning1, null);
        }
        
        [Fact]
        public void ValidationResult_Combine_WithNullResults_HandlesGracefully()
        {
            // Act
            var combined = ValidationResult.Combine(null, ValidationResult.Success(), null);
            
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
            Assert.Throws<ArgumentNullException>(() => result.Merge(null));
        }
        
        [Fact]
        public void ValidationError_WithNullMessage_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationError(null));
        }
        
        [Fact]
        public void ValidationWarning_WithNullMessage_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationWarning(null));
        }
        
        [Fact]
        public void ValidationRule_WithNullParameters_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationRule(null, "description", ValidationType.Required, "expression", "error", ValidationSeverity.Error));
            Assert.Throws<ArgumentNullException>(() => new ValidationRule("name", null, ValidationType.Required, "expression", "error", ValidationSeverity.Error));
            Assert.Throws<ArgumentNullException>(() => new ValidationRule("name", "description", ValidationType.Required, null, "error", ValidationSeverity.Error));
            Assert.Throws<ArgumentNullException>(() => new ValidationRule("name", "description", ValidationType.Required, "expression", null, ValidationSeverity.Error));
        }
    }
} 