using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Template management for PipelineConfigurationService.
    /// </summary>
    public partial class PipelineConfigurationService
    {
        /// <summary>
        /// Initializes default pipeline templates
        /// </summary>
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

        /// <summary>
        /// Clones a pipeline configuration
        /// </summary>
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
                    MaxParallelExecutions = source.Execution?.MaxParallelExecutions ?? 1,
                    CommandTimeoutMs = source.Execution?.CommandTimeoutMs ?? 30000,
                    BehaviorTimeoutMs = source.Execution?.BehaviorTimeoutMs ?? 60000,
                    AggregatorTimeoutMs = source.Execution?.AggregatorTimeoutMs ?? 120000,
                    MaxRetries = source.Execution?.MaxRetries ?? 3,
                    RetryDelayMs = source.Execution?.RetryDelayMs ?? 1000,
                    EnableDetailedLogging = source.Execution?.EnableDetailedLogging ?? false,
                    EnablePerformanceMonitoring = source.Execution?.EnablePerformanceMonitoring ?? false,
                    EnableExecutionHistory = source.Execution?.EnableExecutionHistory ?? false,
                    MaxExecutionHistoryEntries = source.Execution?.MaxExecutionHistoryEntries ?? 100,
                    EnableParallelExecution = source.Execution?.EnableParallelExecution ?? true,
                    EnableDependencyResolution = source.Execution?.EnableDependencyResolution ?? true,
                    EnableResourceManagement = source.Execution?.EnableResourceManagement ?? false,
                    MaxMemoryUsageBytes = source.Execution?.MaxMemoryUsageBytes ?? 1073741824,
                    MaxCpuUsagePercentage = source.Execution?.MaxCpuUsagePercentage ?? 80.0
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
