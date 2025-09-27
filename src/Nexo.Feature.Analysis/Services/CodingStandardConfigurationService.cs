using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;
using Nexo.Feature.Analysis.Services.Configuration;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Orchestrates coding standards configuration management using specialized services.
    /// </summary>
    public partial class CodingStandardConfigurationService : ICodingStandardConfigurationService
    {
        private readonly ILogger<CodingStandardConfigurationService> _logger;
        private readonly ConfigurationValidator _validator;
        private readonly ConfigurationSerializer _serializer;
        private readonly ConfigurationFactory _factory;
        private readonly ConfigurationHistoryManager _historyManager;

        public CodingStandardConfigurationService(ILogger<CodingStandardConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = new ConfigurationValidator(_logger);
            _serializer = new ConfigurationSerializer(_logger);
            _factory = new ConfigurationFactory();
            _historyManager = new ConfigurationHistoryManager(_logger);
        }

        public CodingStandardConfiguration GetCurrentConfiguration()
        {
            return _factory.GetDefaultConfiguration();
        }

        public async Task UpdateConfigurationAsync(CodingStandardConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating coding standards configuration");

            // Validate the configuration
            var validationResult = _validator.ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid configuration: {string.Join(", ", validationResult.Errors)}");
            }

            // Add to history
            _historyManager.AddToHistory(configuration);

            await Task.CompletedTask;
            _logger.LogInformation("Coding standards configuration updated successfully");
        }

        public async Task<CodingStandardConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _serializer.LoadFromFileAsync(filePath, cancellationToken);
        }

        public async Task SaveToFileAsync(CodingStandardConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
        {
            await _serializer.SaveToFileAsync(configuration, filePath, cancellationToken);
        }

        public async Task<CodingStandardConfiguration> LoadFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default)
        {
            var configuration = await _serializer.LoadFromJsonAsync(jsonContent, cancellationToken);
            
            // Validate the loaded configuration
            var validationResult = _validator.ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid configuration loaded from JSON: {string.Join(", ", validationResult.Errors)}");
            }

            return configuration;
        }

        public async Task<string> ToJsonAsync(CodingStandardConfiguration configuration, CancellationToken cancellationToken = default)
        {
            return await _serializer.ToJsonAsync(configuration, cancellationToken);
        }

        public CodingStandardConfigurationValidationResult ValidateConfiguration(CodingStandardConfiguration configuration)
        {
            return _validator.ValidateConfiguration(configuration);
        }

        public CodingStandardConfiguration GetDefaultConfiguration()
        {
            return _factory.GetDefaultConfiguration();
        }

        public Dictionary<string, CodingStandardConfiguration> GetPredefinedConfigurations()
        {
            return _factory.GetPredefinedConfigurations();
        }

        public async Task ResetToDefaultAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Resetting coding standards configuration to default");

            var defaultConfig = _factory.GetDefaultConfiguration();
            await UpdateConfigurationAsync(defaultConfig, cancellationToken);
        }

        public async Task<List<CodingStandardConfiguration>> GetConfigurationHistoryAsync()
        {
            return await _historyManager.GetConfigurationHistoryAsync();
        }

        public async Task RestoreFromHistoryAsync(string configurationId, CancellationToken cancellationToken = default)
        {
            await _historyManager.RestoreFromHistoryAsync(configurationId, cancellationToken);
        }

    }
}
