using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Configuration operations for PipelineConfigurationService.
    /// </summary>
    public partial class PipelineConfigurationService
    {
        /// <summary>
        /// Saves pipeline configuration to a file
        /// </summary>
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

        /// <summary>
        /// Merges multiple pipeline configurations
        /// </summary>
        public Task<PipelineConfiguration> MergeAsync(IEnumerable<PipelineConfiguration> configurations, CancellationToken cancellationToken = default)
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
                return Task.FromResult(merged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging pipeline configurations");
                throw;
            }
        }

        /// <summary>
        /// Gets available pipeline templates
        /// </summary>
        public Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting available pipeline templates");
                var templates = _templates.Keys.ToList();
                _logger.LogInformation("Found {Count} available templates", templates.Count.ToString());
                return Task.FromResult(templates.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available templates");
                throw;
            }
        }

        /// <summary>
        /// Gets template documentation
        /// </summary>
        public Task<string> GetTemplateDocumentationAsync(string templateName, CancellationToken cancellationToken = default)
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
                return Task.FromResult(documentation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template documentation: {TemplateName}", templateName);
                throw;
            }
        }
    }
}
