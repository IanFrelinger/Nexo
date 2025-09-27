using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Configuration validation for PipelineConfigurationService.
    /// </summary>
    public partial class PipelineConfigurationService
    {
        /// <summary>
        /// Validates a pipeline configuration
        /// </summary>
        public Task<Models.PipelineValidationResult> ValidateAsync(PipelineConfiguration configuration, CancellationToken cancellationToken = default)
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
                if (configuration.Execution?.MaxParallelExecutions <= 0)
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

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating pipeline configuration: {Name}", configuration.Name);
                throw;
            }
        }
    }
}
