using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Service for managing workflow configurations and providing default templates.
    /// </summary>
    public class WorkflowConfigurationService : IWorkflowConfigurationService
    {
        private readonly ILogger<WorkflowConfigurationService> _logger;
        private readonly Dictionary<string, WorkflowConfiguration> _templates;

        public WorkflowConfigurationService(ILogger<WorkflowConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templates = new Dictionary<string, WorkflowConfiguration>();
            InitializeDefaultTemplates();
        }

        public async Task<WorkflowConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            try
            {
                _logger.LogInformation("Loading workflow configuration from file: {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Workflow configuration file not found: {filePath}");
                }

                var json = await Task.Run(() => File.ReadAllText(filePath), cancellationToken);
                var configuration = JsonSerializer.Deserialize<WorkflowConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (configuration == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize workflow configuration from: {filePath}");
                }

                _logger.LogInformation("Successfully loaded workflow configuration: {Name}", configuration.Name);
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading workflow configuration from file: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<WorkflowConfiguration> LoadFromJsonAsync(string json, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON cannot be null or empty", nameof(json));

            try
            {
                _logger.LogInformation("Loading workflow configuration from JSON");
                
                var configuration = JsonSerializer.Deserialize<WorkflowConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (configuration == null)
                {
                    throw new InvalidOperationException("Failed to deserialize workflow configuration from JSON");
                }

                _logger.LogInformation("Successfully loaded workflow configuration from JSON: {Name}", configuration.Name);
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading workflow configuration from JSON");
                throw;
            }
        }

        public async Task SaveToFileAsync(WorkflowConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            try
            {
                _logger.LogInformation("Saving workflow configuration to file: {FilePath}", filePath);
                
                var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await Task.Run(() => File.WriteAllText(filePath, json), cancellationToken);
                
                _logger.LogInformation("Successfully saved workflow configuration: {Name}", configuration.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving workflow configuration to file: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<WorkflowConfiguration> GetDefaultConfigurationAsync(WorkflowType type, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting default configuration for workflow type: {Type}", type);

            var templateName = $"default-{type.ToString().ToLowerInvariant()}";
            if (_templates.TryGetValue(templateName, out var template))
            {
                // Clone the template to avoid modifying the original
                return CloneConfiguration(template);
            }

            // Create a basic default configuration
            return CreateDefaultConfiguration(type);
        }

        public async Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return _templates.Keys.ToList();
        }

        public async Task<string> GetTemplateDocumentationAsync(string templateName, CancellationToken cancellationToken = default)
        {
            if (_templates.TryGetValue(templateName, out var template))
            {
                return template.Description;
            }

            return $"Template '{templateName}' not found.";
        }

        public async Task<WorkflowValidationResult> ValidateAsync(WorkflowConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var result = new WorkflowValidationResult { IsValid = true };

            // Validate basic properties
            if (string.IsNullOrEmpty(configuration.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Configuration name is required");
            }

            if (configuration.Steps == null || !configuration.Steps.Any())
            {
                result.IsValid = false;
                result.Errors.Add("Configuration must have at least one step");
            }

            // Validate steps
            if (configuration.Steps != null)
            {
                var stepIds = new HashSet<string>();
                foreach (var step in configuration.Steps)
                {
                    // Check for duplicate step IDs
                    if (!stepIds.Add(step.Id))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Duplicate step ID found: {step.Id}");
                    }

                    // Validate step properties
                    if (string.IsNullOrEmpty(step.Name))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Step {step.Id} must have a name");
                    }

                    if (string.IsNullOrEmpty(step.Command) && step.Type != StepType.Custom)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Step {step.Name} must have a command");
                    }

                    // Validate dependencies
                    foreach (var dependencyId in step.Dependencies)
                    {
                        if (!configuration.Steps.Any(s => s.Id == dependencyId))
                        {
                            result.IsValid = false;
                            result.Errors.Add($"Step {step.Name} depends on non-existent step: {dependencyId}");
                        }
                    }
                }

                // Check for circular dependencies
                if (HasCircularDependencies(configuration.Steps))
                {
                    result.IsValid = false;
                    result.Errors.Add("Circular dependencies detected in workflow steps");
                }
            }

            return result;
        }

        private void InitializeDefaultTemplates()
        {
            // Setup workflow template
            var setupTemplate = new WorkflowConfiguration
            {
                Name = "Default Setup Workflow",
                Description = "Default workflow for setting up a new development environment",
                Type = WorkflowType.Setup,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Check Prerequisites",
                        Description = "Check if required tools and dependencies are installed",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "--version" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Restore Dependencies",
                        Description = "Restore NuGet packages and project dependencies",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "restore" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Build Project",
                        Description = "Build the project to ensure everything compiles",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "build" },
                        IsRequired = true
                    }
                }
            };

            // Analyze workflow template
            var analyzeTemplate = new WorkflowConfiguration
            {
                Name = "Default Analysis Workflow",
                Description = "Default workflow for code analysis and quality checks",
                Type = WorkflowType.Analyze,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Code Analysis",
                        Description = "Run static code analysis",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "analyze" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Style Check",
                        Description = "Check code style and formatting",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "format", "--verify-no-changes" },
                        IsRequired = false
                    }
                }
            };

            // Test workflow template
            var testTemplate = new WorkflowConfiguration
            {
                Name = "Default Test Workflow",
                Description = "Default workflow for running tests and generating reports",
                Type = WorkflowType.Test,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Run Tests",
                        Description = "Run all unit tests",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "test" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Generate Coverage Report",
                        Description = "Generate code coverage report",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "test", "--collect", "XPlat Code Coverage" },
                        IsRequired = false
                    }
                }
            };

            // Deploy workflow template
            var deployTemplate = new WorkflowConfiguration
            {
                Name = "Default Deploy Workflow",
                Description = "Default workflow for building and deploying applications",
                Type = WorkflowType.Deploy,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Build Release",
                        Description = "Build the project in release configuration",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "build", "--configuration", "Release" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Run Tests",
                        Description = "Run tests before deployment",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "test" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Publish Application",
                        Description = "Publish the application for deployment",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "publish", "--configuration", "Release" },
                        IsRequired = true
                    }
                }
            };

            _templates["default-setup"] = setupTemplate;
            _templates["default-analyze"] = analyzeTemplate;
            _templates["default-test"] = testTemplate;
            _templates["default-deploy"] = deployTemplate;
        }

        private WorkflowConfiguration CreateDefaultConfiguration(WorkflowType type)
        {
            return new WorkflowConfiguration
            {
                Name = $"Default {type} Workflow",
                Description = $"Default workflow for {type.ToString().ToLowerInvariant()} operations",
                Type = type,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = $"Default {type} Step",
                        Description = $"Default step for {type.ToString().ToLowerInvariant()} workflow",
                        Type = StepType.Command,
                        Command = "echo",
                        Arguments = new List<string> { $"Running {type} workflow" },
                        IsRequired = true
                    }
                }
            };
        }

        private WorkflowConfiguration CloneConfiguration(WorkflowConfiguration source)
        {
            var json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<WorkflowConfiguration>(json);
        }

        private bool HasCircularDependencies(List<WorkflowStep> steps)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var step in steps)
            {
                if (!visited.Contains(step.Id))
                {
                    if (HasCycle(step.Id, steps, visited, recursionStack))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasCycle(string stepId, List<WorkflowStep> steps, HashSet<string> visited, HashSet<string> recursionStack)
        {
            if (recursionStack.Contains(stepId))
            {
                return true;
            }

            if (visited.Contains(stepId))
            {
                return false;
            }

            visited.Add(stepId);
            recursionStack.Add(stepId);

            var step = steps.FirstOrDefault(s => s.Id == stepId);
            if (step != null)
            {
                foreach (var dependencyId in step.Dependencies)
                {
                    if (HasCycle(dependencyId, steps, visited, recursionStack))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(stepId);
            return false;
        }
    }
}