using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Configuration loading methods for PipelineConfigurationService.
    /// </summary>
    public partial class PipelineConfigurationService
    {
        /// <summary>
        /// Loads pipeline configuration from a file
        /// </summary>
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

        /// <summary>
        /// Loads pipeline configuration from JSON string
        /// </summary>
        public Task<PipelineConfiguration> LoadFromJsonAsync(string json, CancellationToken cancellationToken = default)
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
                return Task.FromResult(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pipeline configuration from JSON");
                throw;
            }
        }

        /// <summary>
        /// Loads pipeline configuration from command line arguments
        /// </summary>
        public Task<PipelineConfiguration> LoadFromCommandLineAsync(string[] args, CancellationToken cancellationToken = default)
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
                return Task.FromResult(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pipeline configuration from command line arguments");
                throw;
            }
        }

        /// <summary>
        /// Loads pipeline configuration from a template
        /// </summary>
        public Task<PipelineConfiguration> LoadFromTemplateAsync(string templateName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
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
                return Task.FromResult(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pipeline configuration from template: {TemplateName}", templateName);
                throw;
            }
        }
    }
}
