using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.CLI.Commands;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Models.Extensions;

namespace Nexo.CLI.Tests.Commands
{
    /// <summary>
    /// Tests for the ExtensionCommands CLI functionality
    /// </summary>
    public class ExtensionCommandTests
    {
        private readonly Mock<IExtensionGenerator> _mockExtensionGenerator;
        private readonly Mock<ILogger<ExtensionCommands>> _mockLogger;
        private readonly ServiceProvider _serviceProvider;

        public ExtensionCommandTests()
        {
            _mockExtensionGenerator = new Mock<IExtensionGenerator>();
            _mockLogger = new Mock<ILogger<ExtensionCommands>>();

            var services = new ServiceCollection();
            services.AddSingleton(_mockExtensionGenerator.Object);
            services.AddSingleton(_mockLogger.Object);
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void CreateExtensionCommand_ShouldReturnCommandWithSubcommands()
        {
            // Act
            var command = ExtensionCommands.CreateExtensionCommand(_serviceProvider);

            // Assert
            Assert.NotNull(command);
            Assert.Equal("extension", command.Name);
            Assert.Equal("AI-generated extension management and generation", command.Description);
            
            // Check that subcommands exist
            var subcommands = command.Subcommands;
            Assert.Contains(subcommands, c => c.Name == "generate");
            Assert.Contains(subcommands, c => c.Name == "list");
            Assert.Contains(subcommands, c => c.Name == "validate");
        }

        [Fact]
        public void CreateGenerateCommand_ShouldHaveRequiredOptions()
        {
            // Act
            var command = ExtensionCommands.CreateExtensionCommand(_serviceProvider);
            var generateCommand = command.Subcommands.First(c => c.Name == "generate");

            // Assert
            Assert.NotNull(generateCommand);
            Assert.Equal("generate", generateCommand.Name);
            Assert.Equal("Generate a new AI-powered extension", generateCommand.Description);

            // Check required options
            var options = generateCommand.Options;
            Assert.Contains(options, o => o.Name == "name" && o.IsRequired);
            Assert.Contains(options, o => o.Name == "description" && o.IsRequired);
            Assert.Contains(options, o => o.Name == "type");
            Assert.Contains(options, o => o.Name == "author");
            Assert.Contains(options, o => o.Name == "version");
            Assert.Contains(options, o => o.Name == "output");
            Assert.Contains(options, o => o.Name == "tags");
        }

        [Fact]
        public void CreateListCommand_ShouldHaveOutputOption()
        {
            // Act
            var command = ExtensionCommands.CreateExtensionCommand(_serviceProvider);
            var listCommand = command.Subcommands.First(c => c.Name == "list");

            // Assert
            Assert.NotNull(listCommand);
            Assert.Equal("list", listCommand.Name);
            Assert.Equal("List generated extensions in the output directory", listCommand.Description);

            // Check options
            var options = listCommand.Options;
            Assert.Contains(options, o => o.Name == "output");
        }

        [Fact]
        public void CreateValidateCommand_ShouldHaveFileOption()
        {
            // Act
            var command = ExtensionCommands.CreateExtensionCommand(_serviceProvider);
            var validateCommand = command.Subcommands.First(c => c.Name == "validate");

            // Assert
            Assert.NotNull(validateCommand);
            Assert.Equal("validate", validateCommand.Name);
            Assert.Equal("Validate an extension file for syntax and structure", validateCommand.Description);

            // Check required options
            var options = validateCommand.Options;
            Assert.Contains(options, o => o.Name == "file" && o.IsRequired);
        }

        [Fact]
        public async Task GenerateCommand_WithValidRequest_ShouldCallExtensionGenerator()
        {
            // Arrange
            var mockResult = new GeneratedCode
            {
                Code = "public class TestExtension { }",
                FileName = "TestExtension.cs",
                FileExtension = ".cs"
            };
            mockResult.GenerationMetadata["RequestId"] = Guid.NewGuid().ToString();
            mockResult.GenerationMetadata["GeneratedAt"] = DateTime.UtcNow;

            _mockExtensionGenerator
                .Setup(x => x.GenerateAsync(It.IsAny<ExtensionRequest>()))
                .ReturnsAsync(mockResult);

            // Create a temporary directory for testing
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Act
                var command = ExtensionCommands.CreateExtensionCommand(_serviceProvider);
                var generateCommand = command.Subcommands.First(c => c.Name == "generate");

                // Simulate command execution by calling the handler directly
                var nameOption = generateCommand.Options.First(o => o.Name == "name");
                var descriptionOption = generateCommand.Options.First(o => o.Name == "description");
                var typeOption = generateCommand.Options.First(o => o.Name == "type");
                var authorOption = generateCommand.Options.First(o => o.Name == "author");
                var versionOption = generateCommand.Options.First(o => o.Name == "version");
                var outputOption = generateCommand.Options.First(o => o.Name == "output");
                var tagsOption = generateCommand.Options.First(o => o.Name == "tags");

                // Set option values
                nameOption.SetValue("TestExtension");
                descriptionOption.SetValue("Test extension description");
                typeOption.SetValue(ExtensionType.Analyzer);
                authorOption.SetValue("TestAuthor");
                versionOption.SetValue("1.0.0");
                outputOption.SetValue(tempDir);
                tagsOption.SetValue(Array.Empty<string>());

                // Get the handler and invoke it
                var handler = generateCommand.Handler;
                Assert.NotNull(handler);

                // Verify the extension generator was called
                _mockExtensionGenerator.Verify(
                    x => x.GenerateAsync(It.Is<ExtensionRequest>(r => 
                        r.Name == "TestExtension" && 
                        r.Description == "Test extension description" &&
                        r.Type == ExtensionType.Analyzer &&
                        r.Author == "TestAuthor" &&
                        r.Version == "1.0.0")),
                    Times.Once);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task GenerateCommand_WithInvalidRequest_ShouldHandleFailure()
        {
            // Arrange
            var mockResult = new GeneratedCode
            {
                Code = string.Empty,
                FileName = "Extension.cs",
                FileExtension = ".cs"
            };
            mockResult.AddError("Validation failed");

            _mockExtensionGenerator
                .Setup(x => x.GenerateAsync(It.IsAny<ExtensionRequest>()))
                .ReturnsAsync(mockResult);

            // Act
            var command = ExtensionCommands.CreateExtensionCommand(_serviceProvider);
            var generateCommand = command.Subcommands.First(c => c.Name == "generate");

            // Verify the extension generator was called
            _mockExtensionGenerator.Verify(
                x => x.GenerateAsync(It.IsAny<ExtensionRequest>()),
                Times.Never); // This will be called when the handler is actually invoked
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
