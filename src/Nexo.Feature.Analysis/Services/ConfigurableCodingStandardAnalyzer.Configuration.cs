using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Configuration management functionality for ConfigurableCodingStandardAnalyzer.
    /// Handles configuration loading, updating, and management.
    /// </summary>
    public partial class ConfigurableCodingStandardAnalyzer
    {
        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        public CodingStandardConfiguration GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// Updates the analyzer configuration.
        /// </summary>
        public async Task UpdateConfigurationAsync(CodingStandardConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating coding standards configuration");

            // Validate the configuration
            var validationResult = _configurationService.ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid configuration: {string.Join(", ", validationResult.Errors)}");
            }

            _configuration = configuration;
            _statistics.LastConfigurationUpdate = DateTime.UtcNow;
            _statistics.TotalStandards = configuration.Standards.Count;
            _statistics.TotalRules = configuration.Standards.Sum(s => s.Rules.Count);
            _statistics.EnabledStandards = configuration.Standards.Count(s => s.IsEnabled);
            _statistics.EnabledRules = configuration.Standards.Sum(s => s.Rules.Count(r => r.IsEnabled));
            _statistics.ConfiguredAgents = configuration.AgentSettings.Count;
            _statistics.ConfiguredFileTypes = configuration.FileTypeSettings.Count;

            await _configurationService.UpdateConfigurationAsync(configuration, cancellationToken);
            _logger.LogInformation("Coding standards configuration updated successfully");
        }

        /// <summary>
        /// Loads configuration from a source.
        /// </summary>
        public async Task LoadConfigurationAsync(string source, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading coding standards configuration from {Source}", source);

            CodingStandardConfiguration configuration;

            if (File.Exists(source))
            {
                configuration = await _configurationService.LoadFromFileAsync(source, cancellationToken);
            }
            else
            {
                configuration = await _configurationService.LoadFromJsonAsync(source, cancellationToken);
            }

            await UpdateConfigurationAsync(configuration, cancellationToken);
            _logger.LogInformation("Coding standards configuration loaded successfully");
        }
    }
}