using System;
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
    public class PipelineRunCommandSnapshotTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IPipelineOrchestrator> _mockPipelineOrchestrator;
        private readonly Mock<ILogger<PipelineRunCommand>> _mockLogger;

        public PipelineRunCommandSnapshotTests()
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
        public async Task GenerateHumanReport_ShouldMatchExpectedFormat()
        {
            // Arrange
            var outputDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nexo-snapshot-test"));
            var command = new PipelineRunCommand(_mockServiceProvider.Object, _mockLogger.Object);

            var result = new PipelineExecutionResult
            {
                Succeeded = true,
                ArtifactId = "test-artifact-123",
                QualityScore = 0.85,
                Notes = new[] { "Pipeline executed successfully", "All tests passed", "Code quality is good" },
                ExecutionTime = TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(30)),
                OutputDirectory = outputDir.FullName,
                IsDryRun = false
            };

            try
            {
                // Act
                await command.GenerateReportsAsync(result, outputDir);

                // Assert
                var reportPath = Path.Combine(outputDir.FullName, "pipeline-report.txt");
                Assert.True(File.Exists(reportPath));

                var reportContent = await File.ReadAllTextAsync(reportPath);
                
                // Verify key elements are present
                Assert.Contains("=== NEXO PIPELINE EXECUTION REPORT ===", reportContent);
                Assert.Contains("Status: SUCCESS", reportContent);
                Assert.Contains("Artifact ID: test-artifact-123", reportContent);
                Assert.Contains("Quality Score: 0.85", reportContent);
                Assert.Contains("Execution Time: 00:02:30", reportContent);
                Assert.Contains("Pipeline executed successfully", reportContent);
                Assert.Contains("All tests passed", reportContent);
                Assert.Contains("Code quality is good", reportContent);
                Assert.Contains("=== END REPORT ===", reportContent);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(outputDir.FullName))
                    Directory.Delete(outputDir.FullName, true);
            }
        }

        [Fact]
        public async Task GenerateJsonReport_ShouldMatchExpectedFormat()
        {
            // Arrange
            var outputDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nexo-snapshot-test"));
            var command = new PipelineRunCommand(_mockServiceProvider.Object, _mockLogger.Object);

            var result = new PipelineExecutionResult
            {
                Succeeded = true,
                ArtifactId = "test-artifact-456",
                QualityScore = 0.92,
                Notes = new[] { "Pipeline completed", "Performance optimized" },
                ExecutionTime = TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(45)),
                OutputDirectory = outputDir.FullName,
                IsDryRun = false
            };

            try
            {
                // Act
                await command.GenerateReportsAsync(result, outputDir);

                // Assert
                var reportPath = Path.Combine(outputDir.FullName, "pipeline-report.json");
                Assert.True(File.Exists(reportPath));

                var reportContent = await File.ReadAllTextAsync(reportPath);
                
                // Verify JSON structure
                Assert.Contains("\"succeeded\": true", reportContent);
                Assert.Contains("\"artifactId\": \"test-artifact-456\"", reportContent);
                Assert.Contains("\"qualityScore\": 0.92", reportContent);
                Assert.Contains("\"executionTime\": \"00:01:45\"", reportContent);
                Assert.Contains("\"isDryRun\": false", reportContent);
                Assert.Contains("\"Pipeline completed\"", reportContent);
                Assert.Contains("\"Performance optimized\"", reportContent);
                Assert.Contains("\"version\": \"1.0.0\"", reportContent);
                Assert.Contains("\"tool\": \"nexo-cli\"", reportContent);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(outputDir.FullName))
                    Directory.Delete(outputDir.FullName, true);
            }
        }

        [Fact]
        public async Task GenerateReports_WithFailedExecution_ShouldShowError()
        {
            // Arrange
            var outputDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nexo-snapshot-test"));
            var command = new PipelineRunCommand(_mockServiceProvider.Object, _mockLogger.Object);

            var result = new PipelineExecutionResult
            {
                Succeeded = false,
                ArtifactId = "failed-artifact-789",
                QualityScore = 0.0,
                Notes = new[] { "Pipeline failed", "Compilation errors found" },
                ExecutionTime = TimeSpan.FromSeconds(30),
                OutputDirectory = outputDir.FullName,
                IsDryRun = false,
                Error = "Compilation failed: Missing dependency"
            };

            try
            {
                // Act
                await command.GenerateReportsAsync(result, outputDir);

                // Assert
                var reportPath = Path.Combine(outputDir.FullName, "pipeline-report.txt");
                Assert.True(File.Exists(reportPath));

                var reportContent = await File.ReadAllTextAsync(reportPath);
                
                // Verify error reporting
                Assert.Contains("Status: FAILED", reportContent);
                Assert.Contains("ERRORS", reportContent);
                Assert.Contains("Compilation failed: Missing dependency", reportContent);
                Assert.Contains("Pipeline failed", reportContent);
                Assert.Contains("Compilation errors found", reportContent);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(outputDir.FullName))
                    Directory.Delete(outputDir.FullName, true);
            }
        }

        [Fact]
        public async Task GenerateReports_WithDryRun_ShouldIndicateDryRun()
        {
            // Arrange
            var outputDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nexo-snapshot-test"));
            var command = new PipelineRunCommand(_mockServiceProvider.Object, _mockLogger.Object);

            var result = new PipelineExecutionResult
            {
                Succeeded = true,
                ArtifactId = "dry-run-artifact-000",
                QualityScore = 1.0,
                Notes = new[] { "Dry run validation successful" },
                ExecutionTime = TimeSpan.Zero,
                OutputDirectory = "N/A (dry run)",
                IsDryRun = true
            };

            try
            {
                // Act
                await command.GenerateReportsAsync(result, outputDir);

                // Assert
                var reportPath = Path.Combine(outputDir.FullName, "pipeline-report.txt");
                Assert.True(File.Exists(reportPath));

                var reportContent = await File.ReadAllTextAsync(reportPath);
                
                // Verify dry run indication
                Assert.Contains("Dry Run: True", reportContent);
                Assert.Contains("Output Directory: N/A (dry run)", reportContent);
                Assert.Contains("Dry run validation successful", reportContent);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(outputDir.FullName))
                    Directory.Delete(outputDir.FullName, true);
            }
        }
    }
}
