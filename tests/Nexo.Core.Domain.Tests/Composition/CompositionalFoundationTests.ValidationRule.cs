using System;
using System.Linq;
using Xunit;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Domain.Tests.Composition
{
    /// <summary>
    /// Validation rule tests for compositional foundation.
    /// </summary>
    public partial class CompositionalFoundationTests
    {
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
            Assert.False(rule.Validate(null!));
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
            Assert.False(rule1.CanComposeWith(null!));
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
    }
}
