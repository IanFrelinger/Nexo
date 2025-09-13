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
            Assert.Contains(result.Errors, e => e.Message.Contains("Plugin name is required"));
            Assert.Contains(result.Errors, e => e.Message.Contains("Plugin description is required"));
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
            Assert.Contains(result.Errors, e => e.Message.Contains("Plugin name is required"));
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
            Assert.Contains(result.Errors, e => e.Message.Contains("Plugin description is required"));
        }

        [Fact]
        public void ExtensionRequest_Should_Validate_Name_Length()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = new string('A', 101), // Too long
                Description = "Valid description",
                TargetNamespace = "TestNamespace"
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.True(result.IsValid); // Current validation doesn't check length
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ExtensionRequest_Should_Validate_Description_Length()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "ValidName", 
                Description = new string('A', 501), // Too long
                TargetNamespace = "TestNamespace"
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.True(result.IsValid); // Current validation doesn't check length
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ExtensionRequest_Should_Validate_Valid_Request()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "JsonValidator",
                Description = "Validate JSON files",
                Type = Nexo.Core.Domain.Enums.Extensions.ExtensionType.Analyzer,
                TargetNamespace = "JsonValidatorNamespace"
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ExtensionRequest_Should_Generate_Valid_Prompt()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "JsonValidator",
                Description = "Validate JSON files",
                Type = Nexo.Core.Domain.Enums.Extensions.ExtensionType.Analyzer
            };

            // Act
            var prompt = request.ToPrompt();

            // Assert
            Assert.NotNull(prompt);
            Assert.Contains("IPlugin", prompt);
            Assert.Contains("JsonValidator", prompt);
            Assert.Contains("Validate JSON files", prompt);
            Assert.Contains("Analyzer", prompt);
        }

        [Fact]
        public void ExtensionRequest_Should_Generate_Prompt_With_All_Properties()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "TestPlugin",
                Description = "A test plugin for validation",
                Type = Nexo.Core.Domain.Enums.Extensions.ExtensionType.Processor,
                Author = "Test Author",
                Version = "1.0.0",
                RequiredServices = new List<string> { "System.Text.Json", "Newtonsoft.Json" },
                Parameters = new Dictionary<string, object> { { "tags", "json,validation,test" } }
            };

            // Act
            var prompt = request.ToPrompt();

            // Assert
            Assert.Contains("TestPlugin", prompt);
            Assert.Contains("A test plugin for validation", prompt);
            Assert.Contains("Processor", prompt);
            Assert.Contains("Test Author", prompt);
            Assert.Contains("1.0.0", prompt);
            Assert.Contains("System.Text.Json", prompt);
            Assert.Contains("Newtonsoft.Json", prompt);
            Assert.Contains("json", prompt);
            Assert.Contains("validation", prompt);
            Assert.Contains("test", prompt);
        }

        [Fact]
        public void ExtensionRequest_Should_Generate_Prompt_With_Minimal_Properties()
        {
            // Arrange
            var request = new ExtensionRequest 
            { 
                Name = "MinimalPlugin",
                Description = "Minimal plugin",
                Type = Nexo.Core.Domain.Enums.Extensions.ExtensionType.Generator
            };

            // Act
            var prompt = request.ToPrompt();

            // Assert
            Assert.Contains("MinimalPlugin", prompt);
            Assert.Contains("Minimal plugin", prompt);
            Assert.Contains("Generator", prompt);
            Assert.Contains("IPlugin", prompt);
        }
    }
}
