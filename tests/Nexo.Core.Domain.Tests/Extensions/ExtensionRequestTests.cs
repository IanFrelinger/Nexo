using Xunit;
using Nexo.Core.Domain.Models.Extensions;
using Nexo.Core.Domain.Enums.Extensions;
using System.Linq;

namespace Nexo.Core.Domain.Tests.Extensions
{
    public class ExtensionRequestTests
    {
        [Fact]
        public void ExtensionRequest_Should_Validate_Required_Fields()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "", 
                Description = "" 
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Any(e => e.Message.Contains("Name is required")));
            Assert.True(result.Errors.Any(e => e.Message.Contains("Description is required")));
        }

        [Fact]
        public void ExtensionRequest_Should_Validate_Name_Not_Empty()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "   ", 
                Description = "Valid description" 
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Any(e => e.Message.Contains("Name cannot be empty or whitespace")));
        }

        [Fact]
        public void ExtensionRequest_Should_Validate_Description_Not_Empty()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "ValidName", 
                Description = "   " 
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Any(e => e.Message.Contains("Description cannot be empty or whitespace")));
        }

        [Fact]
        public void ExtensionRequest_Should_Validate_Name_Length()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = new string('A', 101), // Too long
                Description = "Valid description" 
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Any(e => e.Message.Contains("Name cannot exceed 100 characters")));
        }

        [Fact]
        public void ExtensionRequest_Should_Validate_Description_Length()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "ValidName", 
                Description = new string('A', 501) // Too long
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Any(e => e.Message.Contains("Description cannot exceed 500 characters")));
        }

        [Fact]
        public void ExtensionRequest_Should_Validate_Valid_Request()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "JsonValidator",
                Description = "Validate JSON files",
                Type = ExtensionType.Analyzer
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(0, result.Errors.Count);
        }

        [Fact]
        public void ExtensionRequest_Should_Generate_Valid_Prompt()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "JsonValidator",
                Description = "Validate JSON files",
                Type = ExtensionType.Analyzer
            };

            // Act
            var prompt = request.ToPrompt();

            // Assert
            Assert.NotNull(prompt);
            Assert.True(prompt.Contains("IPlugin"));
            Assert.True(prompt.Contains("JsonValidator"));
            Assert.True(prompt.Contains("Validate JSON files"));
            Assert.True(prompt.Contains("Analyzer"));
        }

        [Fact]
        public void ExtensionRequest_Should_Generate_Prompt_With_All_Properties()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "TestPlugin",
                Description = "A test plugin for validation",
                Type = ExtensionType.Processor,
                Author = "Test Author",
                Version = "1.0.0",
                Dependencies = new[] { "System.Text.Json", "Newtonsoft.Json" },
                Tags = new[] { "json", "validation", "test" }
            };

            // Act
            var prompt = request.ToPrompt();

            // Assert
            Assert.True(prompt.Contains("TestPlugin"));
            Assert.True(prompt.Contains("A test plugin for validation"));
            Assert.True(prompt.Contains("Processor"));
            Assert.True(prompt.Contains("Test Author"));
            Assert.True(prompt.Contains("1.0.0"));
            Assert.True(prompt.Contains("System.Text.Json"));
            Assert.True(prompt.Contains("Newtonsoft.Json"));
            Assert.True(prompt.Contains("json"));
            Assert.True(prompt.Contains("validation"));
            Assert.True(prompt.Contains("test"));
        }

        [Fact]
        public void ExtensionRequest_Should_Generate_Prompt_With_Minimal_Properties()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "MinimalPlugin",
                Description = "Minimal plugin",
                Type = ExtensionType.Generator
            };

            // Act
            var prompt = request.ToPrompt();

            // Assert
            Assert.True(prompt.Contains("MinimalPlugin"));
            Assert.True(prompt.Contains("Minimal plugin"));
            Assert.True(prompt.Contains("Generator"));
            Assert.True(prompt.Contains("IPlugin"));
        }
    }
}
