using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Pipeline.Services;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Shared.Models;

namespace Nexo.Feature.Pipeline.Tests
{
    public class PipelineOrchestratorTests
    {
        private readonly ILogger<PipelineOrchestratorTests> _logger;
        private readonly MockPipelineExecutionEngine _mockExecutionEngine;
        private readonly MockPipelineConfigurationService _mockConfigService;
        private readonly PipelineOrchestrator _orchestrator;

        public PipelineOrchestratorTests()
        {
            _logger = new NullLogger<PipelineOrchestratorTests>();
            _mockExecutionEngine = new MockPipelineExecutionEngine();
            _mockConfigService = new MockPipelineConfigurationService();
            _orchestrator = new PipelineOrchestrator(new NullLogger<PipelineOrchestrator>(), _mockExecutionEngine, _mockConfigService);
        }

        [Fact]
        public void Constructor_WithValidDependencies_ShouldCreateInstance()
        {
            // Act & Assert
            Assert.NotNull(_orchestrator);
        }

        [Fact]
        public async Task GetAvailableTemplatesAsync_ShouldReturnTemplates()
        {
            // Arrange
            var expectedTemplates = new List<string> { "template1", "template2" };
            _mockConfigService.SetupTemplates(expectedTemplates);

            // Act
            var result = await _orchestrator.GetAvailableTemplatesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedTemplates.Count, result.Count());
        }

        [Fact]
        public async Task GetPipelineHealthAsync_ShouldReturnHealthStatus()
        {
            // Act
            var result = await _orchestrator.GetPipelineHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.OverallHealth);
            Assert.NotNull(result.ComponentHealth);
        }

        // Mock implementations for testing
        private class MockPipelineExecutionEngine : IPipelineExecutionEngine
        {
            public Task<PipelineExecutionResult> ExecuteAsync(IPipelineContext context, List<string> aggregatorIds, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new PipelineExecutionResult
                {
                    ExecutionId = context.ExecutionId,
                    Status = ExecutionStatus.Completed,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddMinutes(5)
                });
            }

            public Task<PipelineExecutionPlan> GenerateExecutionPlanAsync(IPipelineContext context, List<string> aggregatorIds, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new PipelineExecutionPlan
                {
                    ExecutionId = Guid.NewGuid().ToString(),
                    Phases = new List<PipelineExecutionPhase>(),
                    Dependencies = new List<ExecutionDependency>()
                });
            }

            public Task<ExecutionValidationResult> ValidateDependenciesAsync(PipelineExecutionPlan plan, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ExecutionValidationResult
                {
                    IsValid = true,
                    Errors = new List<ValidationError>(),
                    Warnings = new List<ValidationWarning>()
                });
            }

            public void RegisterAggregator(IAggregator aggregator)
            {
                // Mock implementation
            }

            public void RegisterBehavior(IBehavior behavior)
            {
                // Mock implementation
            }

            public void RegisterCommand(ICommand command)
            {
                // Mock implementation
            }

            public List<ExecutionMetric> GetExecutionMetrics()
            {
                return new List<ExecutionMetric>();
            }

            public void ClearMetrics()
            {
                // Mock implementation
            }
        }

        private class MockPipelineConfigurationService : IPipelineConfigurationService
        {
            private List<string> _templates = new List<string>();

            public void SetupTemplates(List<string> templates)
            {
                _templates = templates;
            }

            public Task<PipelineConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new PipelineConfiguration
                {
                    Name = "TestConfig",
                    Version = "1.0.0",
                    Description = "Test configuration"
                });
            }

            public Task<PipelineConfiguration> LoadFromJsonAsync(string json, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new PipelineConfiguration
                {
                    Name = "TestConfig",
                    Version = "1.0.0",
                    Description = "Test configuration"
                });
            }

            public Task<PipelineConfiguration> LoadFromCommandLineAsync(string[] args, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new PipelineConfiguration
                {
                    Name = "TestConfig",
                    Version = "1.0.0",
                    Description = "Test configuration"
                });
            }

            public Task<PipelineConfiguration> LoadFromTemplateAsync(string templateName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new PipelineConfiguration
                {
                    Name = "TestConfig",
                    Version = "1.0.0",
                    Description = "Test configuration"
                });
            }

            public Task SaveToFileAsync(PipelineConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<Models.PipelineValidationResult> ValidateAsync(PipelineConfiguration configuration, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new Models.PipelineValidationResult
                {
                    IsValid = true,
                    Issues = new List<ValidationIssue>(),
                    Warnings = new List<string>()
                });
            }

            public Task<PipelineConfiguration> MergeAsync(IEnumerable<PipelineConfiguration> configurations, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new PipelineConfiguration
                {
                    Name = "merged-pipeline",
                    Version = "1.0.0"
                });
            }

            public Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IEnumerable<string>>(_templates);
            }

            public Task<string> GetTemplateDocumentationAsync(string templateName, CancellationToken cancellationToken = default)
            {
                return Task.FromResult($"Documentation for {templateName} template");
            }
        }
    }
} 