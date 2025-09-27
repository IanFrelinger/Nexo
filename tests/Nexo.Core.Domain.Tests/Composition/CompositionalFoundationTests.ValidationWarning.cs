using System;
using Xunit;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Domain.Tests.Composition
{
    /// <summary>
    /// Validation warning tests for compositional foundation.
    /// </summary>
    public partial class CompositionalFoundationTests
    {
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
        public void ValidationWarning_Equals_ComparesCorrectly()
        {
            // Arrange
            var warning1 = new ValidationWarning("Test warning", "Property1", "CODE1", WarningSeverity.High);
            var warning2 = new ValidationWarning("Test warning", "Property1", "CODE1", WarningSeverity.High);
            var warning3 = new ValidationWarning("Test warning", "Property1", "CODE1", WarningSeverity.Low);
            
            // Act & Assert
            Assert.Equal(warning1, warning2);
            Assert.NotEqual(warning1, warning3);
            Assert.NotEqual(null!, warning1);
        }
    }
}
