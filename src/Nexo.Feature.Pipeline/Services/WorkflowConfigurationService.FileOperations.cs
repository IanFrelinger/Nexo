using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// File operations functionality for workflow configuration service.
    /// </summary>
    public partial class WorkflowConfigurationService
    {
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

        public Task<WorkflowConfiguration> LoadFromJsonAsync(string json, CancellationToken cancellationToken = default)
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
                return Task.FromResult(configuration);
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
    }
}
