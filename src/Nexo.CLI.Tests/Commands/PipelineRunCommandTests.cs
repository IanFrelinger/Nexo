using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.CLI.Commands;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.CLI.Tests.Commands
{
    public partial class PipelineRunCommandTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IPipelineOrchestrator> _mockPipelineOrchestrator;
        private readonly Mock<ILogger<PipelineRunCommand>> _mockLogger;

        public PipelineRunCommandTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockPipelineOrchestrator = new Mock<IPipelineOrchestrator>();
            _mockLogger = new Mock<ILogger<PipelineRunCommand>>();

            _mockServiceProvider.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);
            _mockServiceScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
            _mockServiceProvider.Setup(x => x.GetRequiredService<IPipelineOrchestrator>()).Returns(_mockPipelineOrchestrator.Object);
        }

        [Fact]
        public void CreateCommand_ShouldReturnCommandWithCorrectName()
        {
            // Act
            var command = PipelineRunCommand.CreateCommand(_mockServiceProvider.Object, _mockLogger.Object);

            // Assert
            Assert.Equal("run", command.Name);
            Assert.Equal("Execute a pipeline with a request file or stdin input", command.Description);
        }

        [Fact]
        public void CreateCommand_ShouldHaveAllRequiredOptions()
        {
            // Act
            var command = PipelineRunCommand.CreateCommand(_mockServiceProvider.Object, _mockLogger.Object);

            // Assert
            Assert.Contains(command.Options, o => o.Name == "request");
            Assert.Contains(command.Options, o => o.Name == "stdin");
            Assert.Contains(command.Options, o => o.Name == "dry-run");
            Assert.Contains(command.Options, o => o.Name == "out");
            Assert.Contains(command.Options, o => o.Name == "max-repairs");
        }

        [Fact]
        public async Task HandleAsync_WithRequestFile_ShouldExecuteSuccessfully()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var yamlContent = """
                name: "Test Pipeline"
                description: "A test pipeline"
                commands:
                  - id: "test-command"
                    name: "Test Command"
                    description: "A test command"
                """;
            await File.WriteAllTextAsync(tempFile, yamlContent);

            var requestFile = new FileInfo(tempFile);
            var outputDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nexo-test-output"));

            var mockResult = new PipelineExecutionResult
            {
                IsSuccess = true,
                ExecutionId = "test-execution-id",
                Status = ExecutionStatus.Completed,
                StartTime = DateTime.UtcNow.AddMinutes(-1),
                EndTime = DateTime.UtcNow
            };

            _mockPipelineOrchestrator
                .Setup(x => x.ExecuteApplicationPipelineAsync(It.IsAny<ApplicationPipelineRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult);

            var command = new PipelineRunCommand(_mockServiceProvider.Object, _mockLogger.Object);

            try
            {
                // Act
                var result = await command.HandleAsync(requestFile, false, false, outputDir, 3);

                // Assert
                Assert.Equal(0, result);
                Assert.True(Directory.Exists(outputDir.FullName));
                Assert.True(File.Exists(Path.Combine(outputDir.FullName, "pipeline-report.txt")));
                Assert.True(File.Exists(Path.Combine(outputDir.FullName, "pipeline-report.json")));
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (Directory.Exists(outputDir.FullName))
                    Directory.Delete(outputDir.FullName, true);
            }
        }

        [Fact]
        public async Task HandleAsync_WithDryRun_ShouldValidateConfiguration()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var yamlContent = """
                name: "Test Pipeline"
                description: "A test pipeline"
                commands:
                  - id: "test-command"
                    name: "Test Command"
                    description: "A test command"
                """;
            await File.WriteAllTextAsync(tempFile, yamlContent);

            var requestFile = new FileInfo(tempFile);
            var outputDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nexo-test-output"));

            var mockValidationResult = new Models.PipelineValidationResult
            {
                IsValid = true,
                Issues = new List<ValidationIssue>()
            };

            _mockPipelineOrchestrator
                .Setup(x => x.ValidatePipelineConfigurationAsync(It.IsAny<PipelineConfiguration>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockValidationResult);

            var command = new PipelineRunCommand(_mockServiceProvider.Object, _mockLogger.Object);

            try
            {
                // Act
                var result = await command.HandleAsync(requestFile, false, true, outputDir, 3);

                // Assert
                Assert.Equal(0, result);
                _mockPipelineOrchestrator.Verify(x => x.ValidatePipelineConfigurationAsync(It.IsAny<PipelineConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (Directory.Exists(outputDir.FullName))
                    Directory.Delete(outputDir.FullName, true);
            }
        }

        [Fact]
        public async Task HandleAsync_WithStdin_ShouldReadFromConsole()
        {
            // Arrange
            var yamlContent = """
                name: "Test Pipeline"
                description: "A test pipeline"
                commands:
                  - id: "test-command"
                    name: "Test Command"
                    description: "A test command"
                """;

            var outputDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nexo-test-output"));

            var mockResult = new PipelineExecutionResult
            {
                IsSuccess = true,
                ExecutionId = "test-execution-id",
                Status = ExecutionStatus.Completed,
                StartTime = DateTime.UtcNow.AddMinutes(-1),
                EndTime = DateTime.UtcNow
            };

            _mockPipelineOrchestrator
                .Setup(x => x.ExecuteApplicationPipelineAsync(It.IsAny<ApplicationPipelineRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult);

            var command = new PipelineRunCommand(_mockServiceProvider.Object, _mockLogger.Object);

            // Mock Console.In
            var originalIn = Console.In;
            var stringReader = new StringReader(yamlContent);
            Console.SetIn(stringReader);

            try
            {
                // Act
                var result = await command.HandleAsync(null, true, false, outputDir, 3);

                // Assert
                Assert.Equal(0, result);
            }
            finally
            {
                // Cleanup
                Console.SetIn(originalIn);
                if (Directory.Exists(outputDir.FullName))
                    Directory.Delete(outputDir.FullName, true);
            }
        }

        [Fact]
        public async Task HandleAsync_WithInvalidRequest_ShouldReturnError()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var invalidContent = "invalid yaml content {";
            await File.WriteAllTextAsync(tempFile, invalidContent);

            var requestFile = new FileInfo(tempFile);
            var outputDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nexo-test-output"));

            var command = new PipelineRunCommand(_mockServiceProvider.Object, _mockLogger.Object);

            try
            {
                // Act
                var result = await command.HandleAsync(requestFile, false, false, outputDir, 3);

                // Assert
                Assert.Equal(1, result);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (Directory.Exists(outputDir.FullName))
                    Directory.Delete(outputDir.FullName, true);
            }
        }

        [Fact]
        public async Task HandleAsync_WithPipelineExecutionError_ShouldReturnError()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var yamlContent = """
                name: "Test Pipeline"
                description: "A test pipeline"
                commands:
                  - id: "test-command"
                    name: "Test Command"
                    description: "A test command"
                """;
            await File.WriteAllTextAsync(tempFile, yamlContent);

            var requestFile = new FileInfo(tempFile);
            var outputDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nexo-test-output"));

            _mockPipelineOrchestrator
                .Setup(x => x.ExecuteApplicationPipelineAsync(It.IsAny<ApplicationPipelineRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Pipeline execution failed"));

            var command = new PipelineRunCommand(_mockServiceProvider.Object, _mockLogger.Object);

            try
            {
                // Act
                var result = await command.HandleAsync(requestFile, false, false, outputDir, 3);

                // Assert
                Assert.Equal(1, result);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (Directory.Exists(outputDir.FullName))
                    Directory.Delete(outputDir.FullName, true);
            }
        }
    }
}
