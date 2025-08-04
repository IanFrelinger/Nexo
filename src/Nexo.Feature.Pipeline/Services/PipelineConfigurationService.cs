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
    /// Service for managing pipeline configurations from various sources.
    /// </summary>
    public class PipelineConfigurationService : IPipelineConfigurationService
    {
        private readonly ILogger<PipelineConfigurationService> _logger;
        private readonly Dictionary<string, PipelineConfiguration> _templates;

        public PipelineConfigurationService(ILogger<PipelineConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templates = new Dictionary<string, PipelineConfiguration>();
            InitializeDefaultTemplates();
        }

        public async Task<PipelineConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            try
            {
                _logger.LogInformation("Loading pipeline configuration from file: {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Pipeline configuration file not found: {filePath}");
                }

                var json = await Task.Run(() => File.ReadAllText(filePath), cancellationToken);
                var configuration = JsonSerializer.Deserialize<PipelineConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (configuration == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize pipeline configuration from: {filePath}");
                }

                _logger.LogInformation("Successfully loaded pipeline configuration: {Name}", configuration.Name);
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pipeline configuration from file: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<PipelineConfiguration> LoadFromJsonAsync(string json, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON cannot be null or empty", nameof(json));

            try
            {
                _logger.LogInformation("Loading pipeline configuration from JSON");
                
                var configuration = JsonSerializer.Deserialize<PipelineConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (configuration == null)
                {
                    throw new InvalidOperationException("Failed to deserialize pipeline configuration from JSON");
                }

                _logger.LogInformation("Successfully loaded pipeline configuration from JSON: {Name}", configuration.Name);
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pipeline configuration from JSON");
                throw;
            }
        }

        public async Task<PipelineConfiguration> LoadFromCommandLineAsync(string[] args, CancellationToken cancellationToken = default)
        {
            if (args == null || args.Length == 0)
                throw new ArgumentException("Command line arguments cannot be null or empty", nameof(args));

            try
            {
                _logger.LogInformation("Loading pipeline configuration from command line arguments");
                
                var configuration = new PipelineConfiguration
                {
                    Name = "CommandLinePipeline",
                    Version = "1.0.0",
                    Description = "Pipeline configuration generated from command line arguments",
                    Author = "Nexo CLI",
                    Tags = new List<string> { "cli", "command-line" },
                    Execution = new PipelineExecutionSettings(),
                    Commands = new List<PipelineCommandConfiguration>(),
                    Behaviors = new List<PipelineBehaviorConfiguration>(),
                    Aggregators = new List<PipelineAggregatorConfiguration>(),
                    Variables = new Dictionary<string, object>(),
                    Environments = new Dictionary<string, PipelineEnvironmentConfiguration>(),
                    Validation = new PipelineValidationConfiguration(),
                    Documentation = new PipelineDocumentationConfiguration()
                };

                // Parse command line arguments and populate configuration
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    if (arg.StartsWith("--"))
                    {
                        var key = arg.Substring(2);
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                        {
                            var value = args[i + 1];
                            configuration.Variables[key] = value;
                            i++; // Skip the value in next iteration
                        }
                        else
                        {
                            configuration.Variables[key] = true;
                        }
                    }
                }

                _logger.LogInformation("Successfully created pipeline configuration from command line arguments");
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pipeline configuration from command line arguments");
                throw;
            }
        }

        public async Task<PipelineConfiguration> LoadFromTemplateAsync(string templateName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(templateName))
                throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));

            try
            {
                _logger.LogInformation("Loading pipeline configuration from template: {TemplateName}", templateName);
                
                if (!_templates.ContainsKey(templateName))
                {
                    throw new ArgumentException($"Template not found: {templateName}");
                }

                var template = _templates[templateName];
                var configuration = CloneConfiguration(template);

                // Apply parameters
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        configuration.Variables[param.Key] = param.Value;
                    }
                }

                _logger.LogInformation("Successfully loaded pipeline configuration from template: {TemplateName}", templateName);
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pipeline configuration from template: {TemplateName}", templateName);
                throw;
            }
        }

        public async Task SaveToFileAsync(PipelineConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            try
            {
                _logger.LogInformation("Saving pipeline configuration to file: {FilePath}", filePath);
                
                var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await Task.Run(() => File.WriteAllText(filePath, json), cancellationToken);
                
                _logger.LogInformation("Successfully saved pipeline configuration to file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving pipeline configuration to file: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<Models.PipelineValidationResult> ValidateAsync(PipelineConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            try
            {
                _logger.LogInformation("Validating pipeline configuration: {Name}", configuration.Name);
                
                var result = new Models.PipelineValidationResult { IsValid = true };

                // Validate basic properties
                if (string.IsNullOrEmpty(configuration.Name))
                {
                    result.IsValid = false;
                    result.Issues.Add(new Models.ValidationIssue
                    {
                        Field = "name",
                        Message = "Pipeline name is required",
                        Severity = "Error",
                        Recommendation = "Provide a valid pipeline name"
                    });
                }

                if (string.IsNullOrEmpty(configuration.Version))
                {
                    result.IsValid = false;
                    result.Issues.Add(new Models.ValidationIssue
                    {
                        Field = "version",
                        Message = "Pipeline version is required",
                        Severity = "Error",
                        Recommendation = "Provide a valid pipeline version"
                    });
                }

                // Validate execution settings
                if (configuration.Execution.MaxParallelExecutions <= 0)
                {
                    result.IsValid = false;
                    result.Issues.Add(new Models.ValidationIssue
                    {
                        Field = "execution.maxParallelExecutions",
                        Message = "Max parallel executions must be greater than 0",
                        Severity = "Error",
                        Recommendation = "Set maxParallelExecutions to a value greater than 0"
                    });
                }

                // Validate commands
                foreach (var command in configuration.Commands)
                {
                    if (string.IsNullOrEmpty(command.Id))
                    {
                        result.IsValid = false;
                        result.Issues.Add(new Models.ValidationIssue
                        {
                            Field = $"commands[{command.Name}]",
                            Message = "Command ID is required",
                            Severity = "Error",
                            Recommendation = "Provide a valid command ID"
                        });
                    }
                }

                // Validate behaviors
                foreach (var behavior in configuration.Behaviors)
                {
                    if (string.IsNullOrEmpty(behavior.Id))
                    {
                        result.IsValid = false;
                        result.Issues.Add(new Models.ValidationIssue
                        {
                            Field = $"behaviors[{behavior.Name}]",
                            Message = "Behavior ID is required",
                            Severity = "Error",
                            Recommendation = "Provide a valid behavior ID"
                        });
                    }
                }

                // Validate aggregators
                foreach (var aggregator in configuration.Aggregators)
                {
                    if (string.IsNullOrEmpty(aggregator.Id))
                    {
                        result.IsValid = false;
                        result.Issues.Add(new Models.ValidationIssue
                        {
                            Field = $"aggregators[{aggregator.Name}]",
                            Message = "Aggregator ID is required",
                            Severity = "Error",
                            Recommendation = "Provide a valid aggregator ID"
                        });
                    }
                }

                _logger.LogInformation("Pipeline configuration validation completed. IsValid: {IsValid}, Issues: {IssueCount}, Warnings: {WarningCount}", 
                    result.IsValid, result.Issues.Count, result.Warnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating pipeline configuration: {Name}", configuration.Name);
                throw;
            }
        }

        public async Task<PipelineConfiguration> MergeAsync(IEnumerable<PipelineConfiguration> configurations, CancellationToken cancellationToken = default)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            try
            {
                _logger.LogInformation("Merging pipeline configurations");
                
                var configList = new List<PipelineConfiguration>(configurations);
                if (configList.Count == 0)
                {
                    throw new ArgumentException("At least one configuration must be provided");
                }

                var merged = CloneConfiguration(configList[0]);
                merged.Name = "MergedPipeline";
                merged.Description = "Pipeline configuration merged from multiple sources";

                // Merge additional configurations
                for (int i = 1; i < configList.Count; i++)
                {
                    var config = configList[i];
                    
                    // Merge commands
                    foreach (var command in config.Commands)
                    {
                        if (!merged.Commands.Exists(c => c.Id == command.Id))
                        {
                            merged.Commands.Add(command);
                        }
                    }

                    // Merge behaviors
                    foreach (var behavior in config.Behaviors)
                    {
                        if (!merged.Behaviors.Exists(b => b.Id == behavior.Id))
                        {
                            merged.Behaviors.Add(behavior);
                        }
                    }

                    // Merge aggregators
                    foreach (var aggregator in config.Aggregators)
                    {
                        if (!merged.Aggregators.Exists(a => a.Id == aggregator.Id))
                        {
                            merged.Aggregators.Add(aggregator);
                        }
                    }

                    // Merge variables
                    foreach (var variable in config.Variables)
                    {
                        merged.Variables[variable.Key] = variable.Value;
                    }

                    // Merge environments
                    foreach (var environment in config.Environments)
                    {
                        merged.Environments[environment.Key] = environment.Value;
                    }
                }

                _logger.LogInformation("Successfully merged {Count} pipeline configurations", configList.Count);
                return merged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging pipeline configurations");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting available pipeline templates");
                var templates = _templates.Keys.ToList();
                _logger.LogInformation("Found {Count} available templates", templates.Count.ToString());
                return templates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available templates");
                throw;
            }
        }

        public async Task<string> GetTemplateDocumentationAsync(string templateName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(templateName))
                throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));

            try
            {
                _logger.LogInformation("Getting documentation for template: {TemplateName}", templateName);
                
                if (!_templates.ContainsKey(templateName))
                {
                    throw new ArgumentException($"Template not found: {templateName}");
                }

                var template = _templates[templateName];
                var documentation = $@"
# {template.Name} Pipeline Template

## Description
{template.Description}

## Version
{template.Version}

## Author
{template.Author}

## Tags
{string.Join(", ", template.Tags)}

## Commands
{string.Join("\n", template.Commands.Select(c => $"- {c.Name}: {c.Description}"))}

## Behaviors
{string.Join("\n", template.Behaviors.Select(b => $"- {b.Name}: {b.Description}"))}

## Aggregators
{string.Join("\n", template.Aggregators.Select(a => $"- {a.Name}: {a.Description}"))}

## Usage
Use this template with: `nexo pipeline create {templateName} --param key=value`
";

                _logger.LogInformation("Successfully retrieved documentation for template: {TemplateName}", templateName);
                return documentation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template documentation: {TemplateName}", templateName);
                throw;
            }
        }

        private void InitializeDefaultTemplates()
        {
            // Web API Template
            _templates["webapi"] = new PipelineConfiguration
            {
                Name = "Web API Pipeline",
                Version = "1.0.0",
                Description = "Pipeline for creating and managing ASP.NET Core Web API projects",
                Author = "Nexo Team",
                Tags = new List<string> { "webapi", "aspnet", "rest" },
                Execution = new PipelineExecutionSettings
                {
                    MaxParallelExecutions = 4,
                    CommandTimeoutMs = 30000,
                    EnableDetailedLogging = true,
                    EnablePerformanceMonitoring = true
                },
                Commands = new List<PipelineCommandConfiguration>
                {
                    new PipelineCommandConfiguration
                    {
                        Id = "create-project",
                        Name = "Create Project",
                        Description = "Creates a new ASP.NET Core Web API project",
                        Category = "Project",
                        Priority = "High"
                    },
                    new PipelineCommandConfiguration
                    {
                        Id = "add-controllers",
                        Name = "Add Controllers",
                        Description = "Adds API controllers to the project",
                        Category = "Development",
                        Priority = "Normal"
                    }
                },
                Behaviors = new List<PipelineBehaviorConfiguration>
                {
                    new PipelineBehaviorConfiguration
                    {
                        Id = "validate-project",
                        Name = "Validate Project",
                        Description = "Validates the project structure and configuration",
                        ExecutionStrategy = "Sequential"
                    }
                },
                Aggregators = new List<PipelineAggregatorConfiguration>
                {
                    new PipelineAggregatorConfiguration
                    {
                        Id = "webapi-setup",
                        Name = "Web API Setup",
                        Description = "Complete Web API project setup",
                        ExecutionStrategy = "Sequential"
                    }
                }
            };

            // Console Application Template
            _templates["console"] = new PipelineConfiguration
            {
                Name = "Console Application Pipeline",
                Version = "1.0.0",
                Description = "Pipeline for creating and managing .NET Console applications",
                Author = "Nexo Team",
                Tags = new List<string> { "console", "dotnet", "cli" },
                Execution = new PipelineExecutionSettings
                {
                    MaxParallelExecutions = 2,
                    CommandTimeoutMs = 15000,
                    EnableDetailedLogging = true
                },
                Commands = new List<PipelineCommandConfiguration>
                {
                    new PipelineCommandConfiguration
                    {
                        Id = "create-console",
                        Name = "Create Console App",
                        Description = "Creates a new .NET Console application",
                        Category = "Project",
                        Priority = "High"
                    }
                },
                Behaviors = new List<PipelineBehaviorConfiguration>
                {
                    new PipelineBehaviorConfiguration
                    {
                        Id = "validate-console",
                        Name = "Validate Console App",
                        Description = "Validates the console application structure",
                        ExecutionStrategy = "Sequential"
                    }
                },
                Aggregators = new List<PipelineAggregatorConfiguration>
                {
                    new PipelineAggregatorConfiguration
                    {
                        Id = "console-setup",
                        Name = "Console Setup",
                        Description = "Complete console application setup",
                        ExecutionStrategy = "Sequential"
                    }
                }
            };
        }

        private PipelineConfiguration CloneConfiguration(PipelineConfiguration source)
        {
            return new PipelineConfiguration
            {
                Name = source.Name,
                Version = source.Version,
                Description = source.Description,
                Author = source.Author,
                Tags = new List<string>(source.Tags),
                Execution = new PipelineExecutionSettings
                {
                    MaxParallelExecutions = source.Execution.MaxParallelExecutions,
                    CommandTimeoutMs = source.Execution.CommandTimeoutMs,
                    BehaviorTimeoutMs = source.Execution.BehaviorTimeoutMs,
                    AggregatorTimeoutMs = source.Execution.AggregatorTimeoutMs,
                    MaxRetries = source.Execution.MaxRetries,
                    RetryDelayMs = source.Execution.RetryDelayMs,
                    EnableDetailedLogging = source.Execution.EnableDetailedLogging,
                    EnablePerformanceMonitoring = source.Execution.EnablePerformanceMonitoring,
                    EnableExecutionHistory = source.Execution.EnableExecutionHistory,
                    MaxExecutionHistoryEntries = source.Execution.MaxExecutionHistoryEntries,
                    EnableParallelExecution = source.Execution.EnableParallelExecution,
                    EnableDependencyResolution = source.Execution.EnableDependencyResolution,
                    EnableResourceManagement = source.Execution.EnableResourceManagement,
                    MaxMemoryUsageBytes = source.Execution.MaxMemoryUsageBytes,
                    MaxCpuUsagePercentage = source.Execution.MaxCpuUsagePercentage
                },
                Commands = new List<PipelineCommandConfiguration>(source.Commands),
                Behaviors = new List<PipelineBehaviorConfiguration>(source.Behaviors),
                Aggregators = new List<PipelineAggregatorConfiguration>(source.Aggregators),
                Variables = new Dictionary<string, object>(source.Variables),
                Environments = new Dictionary<string, PipelineEnvironmentConfiguration>(source.Environments),
                Validation = new PipelineValidationConfiguration
                {
                    Rules = new List<ValidationRuleConfiguration>(source.Validation.Rules),
                    FailOnError = source.Validation.FailOnError,
                    TimeoutMs = source.Validation.TimeoutMs
                },
                Documentation = new PipelineDocumentationConfiguration
                {
                    Summary = source.Documentation.Summary,
                    Details = source.Documentation.Details,
                    Examples = source.Documentation.Examples,
                    Tags = source.Documentation.Tags,
                    Links = source.Documentation.Links
                }
            };
        }
    }
} 