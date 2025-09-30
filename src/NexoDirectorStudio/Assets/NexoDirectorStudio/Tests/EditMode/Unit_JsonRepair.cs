using NUnit.Framework;
using NexoDirectorStudio.Adapters;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Unit tests for JSON repair functionality.
    /// Tests the JsonRepair utility for fixing malformed JSON responses from AI adapters.
    /// </summary>
    [TestFixture]
    public class Unit_JsonRepair
    {
        [Test]
        public void JsonRepair_ShouldRepairTrailingCommas()
        {
            // Arrange
            var malformedJson = @"{""name"": ""test"", ""value"": 123,}";
            
            // Act
            var result = JsonRepair.Repair(malformedJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair trailing commas");
            Assert.IsNotNull(result.RepairedJson, "Should have repaired JSON");
            Assert.AreNotEqual(result.OriginalJson, result.RepairedJson, "Repaired JSON should be different from original");
        }
        
        [Test]
        public void JsonRepair_ShouldRepairMissingQuotesAroundKeys()
        {
            // Arrange
            var malformedJson = @"{name: ""test"", value: 123}";
            
            // Act
            var result = JsonRepair.Repair(malformedJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair missing quotes around keys");
            Assert.IsNotNull(result.RepairedJson, "Should have repaired JSON");
        }
        
        [Test]
        public void JsonRepair_ShouldRepairSingleQuotesToDoubleQuotes()
        {
            // Arrange
            var malformedJson = @"{'name': 'test', 'value': 123}";
            
            // Act
            var result = JsonRepair.Repair(malformedJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair single quotes");
            Assert.IsNotNull(result.RepairedJson, "Should have repaired JSON");
        }
        
        [Test]
        public void JsonRepair_ShouldRepairMissingCommas()
        {
            // Arrange
            var malformedJson = @"{""name"": ""test"" ""value"": 123}";
            
            // Act
            var result = JsonRepair.Repair(malformedJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair missing commas");
            Assert.IsNotNull(result.RepairedJson, "Should have repaired JSON");
        }
        
        [Test]
        public void JsonRepair_ShouldExtractJsonFromText()
        {
            // Arrange
            var textWithJson = @"Some text before
            {""name"": ""test"", ""value"": 123}
            Some text after";
            
            // Act
            var result = JsonRepair.Repair(textWithJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully extract JSON from text");
            Assert.IsNotNull(result.RepairedJson, "Should have repaired JSON");
        }
        
        [Test]
        public void JsonRepair_ShouldCreateMinimalJson()
        {
            // Arrange
            var malformedJson = @"name: test, value: 123, status: active";
            
            // Act
            var result = JsonRepair.Repair(malformedJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully create minimal JSON");
            Assert.IsNotNull(result.RepairedJson, "Should have repaired JSON");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleValidJson()
        {
            // Arrange
            var validJson = @"{""name"": ""test"", ""value"": 123}";
            
            // Act
            var result = JsonRepair.Repair(validJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should handle valid JSON");
            Assert.AreEqual(validJson, result.RepairedJson, "Valid JSON should remain unchanged");
            Assert.AreEqual(1, result.RepairAttempts, "Should only need one attempt for valid JSON");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleNullInput()
        {
            // Arrange
            string nullJson = null;
            
            // Act
            var result = JsonRepair.Repair(nullJson);
            
            // Assert
            Assert.IsFalse(result.IsSuccessful, "Should fail for null input");
            Assert.IsNotNull(result.ErrorMessage, "Should have error message");
            Assert.IsTrue(result.ErrorMessage.Contains("null or empty"), "Should indicate null input error");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleEmptyInput()
        {
            // Arrange
            var emptyJson = "";
            
            // Act
            var result = JsonRepair.Repair(emptyJson);
            
            // Assert
            Assert.IsFalse(result.IsSuccessful, "Should fail for empty input");
            Assert.IsNotNull(result.ErrorMessage, "Should have error message");
            Assert.IsTrue(result.ErrorMessage.Contains("null or empty"), "Should indicate empty input error");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleWhitespaceInput()
        {
            // Arrange
            var whitespaceJson = "   \n\t   ";
            
            // Act
            var result = JsonRepair.Repair(whitespaceJson);
            
            // Assert
            Assert.IsFalse(result.IsSuccessful, "Should fail for whitespace input");
            Assert.IsNotNull(result.ErrorMessage, "Should have error message");
            Assert.IsTrue(result.ErrorMessage.Contains("null or empty"), "Should indicate whitespace input error");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleUnrepairableJson()
        {
            // Arrange
            var unrepairableJson = "This is not JSON at all, just random text with no structure";
            
            // Act
            var result = JsonRepair.Repair(unrepairableJson);
            
            // Assert
            Assert.IsFalse(result.IsSuccessful, "Should fail for unrepairable JSON");
            Assert.IsNotNull(result.ErrorMessage, "Should have error message");
            Assert.IsTrue(result.ErrorMessage.Contains("All repair attempts failed"), "Should indicate all attempts failed");
            Assert.AreEqual(4, result.RepairAttempts, "Should make 4 repair attempts");
        }
        
        [Test]
        public void JsonRepair_ShouldValidateJson()
        {
            // Arrange
            var validJson = @"{""name"": ""test"", ""value"": 123}";
            var invalidJson = @"{""name"": ""test"", ""value"": 123,}";
            
            // Act
            var validResult = JsonRepair.Validate(validJson);
            var invalidResult = JsonRepair.Validate(invalidJson);
            
            // Assert
            Assert.IsTrue(validResult.IsValid, "Should validate correct JSON");
            Assert.IsFalse(invalidResult.IsValid, "Should invalidate malformed JSON");
            Assert.IsNotNull(invalidResult.ErrorMessage, "Should have error message for invalid JSON");
        }
        
        [Test]
        public void JsonRepair_ShouldRepairWithSchema()
        {
            // Arrange
            var malformedJson = @"{name: ""test"", value: 123}";
            var schema = new JsonSchema
            {
                RequiredFields = new[] { "id", "name", "status" },
                OptionalFields = new[] { "value", "description" }
            };
            
            // Act
            var result = JsonRepair.RepairWithSchema(malformedJson, schema);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair with schema");
            Assert.IsNotNull(result.RepairedJson, "Should have repaired JSON");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleComplexJson()
        {
            // Arrange
            var complexJson = @"{
                name: ""test"",
                value: 123,
                items: [""item1"", ""item2""],
                nested: {
                    prop1: ""value1"",
                    prop2: 456
                }
            }";
            
            // Act
            var result = JsonRepair.Repair(complexJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair complex JSON");
            Assert.IsNotNull(result.RepairedJson, "Should have repaired JSON");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleArrayJson()
        {
            // Arrange
            var arrayJson = @"[""item1"", ""item2"", ""item3""]";
            
            // Act
            var result = JsonRepair.Repair(arrayJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair array JSON");
            Assert.AreEqual(arrayJson, result.RepairedJson, "Valid array JSON should remain unchanged");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleBooleanValues()
        {
            // Arrange
            var booleanJson = @"{""enabled"": true, ""disabled"": false}";
            
            // Act
            var result = JsonRepair.Repair(booleanJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair boolean JSON");
            Assert.AreEqual(booleanJson, result.RepairedJson, "Valid boolean JSON should remain unchanged");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleNullValues()
        {
            // Arrange
            var nullJson = @"{""value"": null, ""name"": ""test""}";
            
            // Act
            var result = JsonRepair.Repair(nullJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair null JSON");
            Assert.AreEqual(nullJson, result.RepairedJson, "Valid null JSON should remain unchanged");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleNumericValues()
        {
            // Arrange
            var numericJson = @"{""integer"": 123, ""decimal"": 45.67, ""negative"": -89}";
            
            // Act
            var result = JsonRepair.Repair(numericJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair numeric JSON");
            Assert.AreEqual(numericJson, result.RepairedJson, "Valid numeric JSON should remain unchanged");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleEscapedCharacters()
        {
            // Arrange
            var escapedJson = @"{""message"": ""Hello \""World\""!"", ""path"": ""C:\\Users\\Test""}";
            
            // Act
            var result = JsonRepair.Repair(escapedJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair escaped JSON");
            Assert.AreEqual(escapedJson, result.RepairedJson, "Valid escaped JSON should remain unchanged");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleUnicodeCharacters()
        {
            // Arrange
            var unicodeJson = @"{""message"": ""Hello 世界!"", ""emoji"": ""🚀""}";
            
            // Act
            var result = JsonRepair.Repair(unicodeJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair unicode JSON");
            Assert.AreEqual(unicodeJson, result.RepairedJson, "Valid unicode JSON should remain unchanged");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleMultipleRepairAttempts()
        {
            // Arrange
            var veryMalformedJson = @"name: test, value: 123, items: [item1, item2], nested: {prop1: value1}";
            
            // Act
            var result = JsonRepair.Repair(veryMalformedJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair very malformed JSON");
            Assert.IsNotNull(result.RepairedJson, "Should have repaired JSON");
            Assert.IsTrue(result.RepairAttempts > 1, "Should require multiple repair attempts");
        }
        
        [Test]
        public void JsonRepair_ShouldPreserveOriginalJson()
        {
            // Arrange
            var originalJson = @"{""name"": ""test"", ""value"": 123}";
            
            // Act
            var result = JsonRepair.Repair(originalJson);
            
            // Assert
            Assert.AreEqual(originalJson, result.OriginalJson, "Should preserve original JSON");
            Assert.AreEqual(originalJson, result.RepairedJson, "Valid JSON should remain unchanged");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleEmptyObject()
        {
            // Arrange
            var emptyObjectJson = @"{}";
            
            // Act
            var result = JsonRepair.Repair(emptyObjectJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair empty object");
            Assert.AreEqual(emptyObjectJson, result.RepairedJson, "Valid empty object should remain unchanged");
        }
        
        [Test]
        public void JsonRepair_ShouldHandleEmptyArray()
        {
            // Arrange
            var emptyArrayJson = @"[]";
            
            // Act
            var result = JsonRepair.Repair(emptyArrayJson);
            
            // Assert
            Assert.IsTrue(result.IsSuccessful, "Should successfully repair empty array");
            Assert.AreEqual(emptyArrayJson, result.RepairedJson, "Valid empty array should remain unchanged");
        }
    }
}
