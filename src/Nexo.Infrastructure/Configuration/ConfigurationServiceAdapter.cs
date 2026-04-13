using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Configuration.Models;
using Nexo.Core.Application.Configuration.Ports;
using Nexo.Core.Domain;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Infrastructure.Configuration;

/// <summary>
/// Infrastructure adapter for configuration management.
/// 
/// Implements IConfigurationService port from Application layer. Provides:
/// - Configuration loading from JSON file (~/.nexo/config.json)
/// - Configuration saving with validation
/// - Default configuration fallback
/// - Error handling for invalid configurations
/// 
/// Part of the Infrastructure layer, implementing the hexagonal architecture pattern.
/// </summary>
public class ConfigurationServiceAdapter : IConfigurationService
{
    private readonly ILogger<ConfigurationServiceAdapter> _logger;
    private readonly string _configFilePath;
    private readonly bool _failOnWarnings;

    public ConfigurationServiceAdapter(ILogger<ConfigurationServiceAdapter> logger, bool failOnConfigWarnings = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _failOnWarnings = failOnConfigWarnings;
        var configPath = Environment.GetEnvironmentVariable("NEXO_CONFIG_PATH");
        _configFilePath = !string.IsNullOrWhiteSpace(configPath)
            ? configPath
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                NexoDefaults.ConfigDirectoryName,
                NexoDefaults.ConfigFileName);
    }

    public async Task<NexoConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configFilePath))
        {
            if (_failOnWarnings)
                throw new ConfigurationException(
                    $"Configuration file not found at {_configFilePath} (strict mode is enabled — create the file or set NEXO_CONFIG_PATH).",
                    ErrorCodes.ConfigFileNotFound);
            _logger.LogInformation("Configuration file not found, using defaults");
            return GetDefault();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            var config = JsonSerializer.Deserialize<NexoConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config == null)
            {
                if (_failOnWarnings)
                    throw new ConfigurationException(
                        $"Configuration file at {_configFilePath} is empty or invalid (strict mode is enabled).",
                        ErrorCodes.ConfigInvalidFormat);
                _logger.LogWarning("Configuration file is empty or invalid, using defaults");
                return GetDefault();
            }

            _logger.LogInformation("Configuration loaded from: {Path}", _configFilePath);
            return config;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse configuration file");
            throw new ConfigurationException(
                $"Invalid configuration file format: {ex.Message}",
                ErrorCodes.ConfigInvalidFormat,
                ex,
                "Check the JSON syntax in the configuration file.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
            throw new ConfigurationException(
                $"Failed to load configuration: {ex.Message}",
                ErrorCodes.ConfigFileNotFound,
                ex);
        }
    }

    public async Task SaveAsync(NexoConfiguration configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(configuration, options);
            await File.WriteAllTextAsync(_configFilePath, json, cancellationToken);

            _logger.LogInformation("Configuration saved to: {Path}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
            throw new ConfigurationException(
                $"Failed to save configuration: {ex.Message}",
                ErrorCodes.ConfigInvalidValue,
                ex);
        }
    }

    public NexoConfiguration GetDefault()
    {
        return new NexoConfiguration
        {
            Analysis = new AnalysisConfiguration
            {
                EnabledRules = new[] { "SecurityScan", "CodeQuality" },
                MaxComplexityThreshold = NexoDefaults.AnalysisMaxComplexityThreshold,
                EnableSecurityScan = true,
                EnableCodeQuality = true
            },
            Validation = new ValidationConfiguration
            {
                TimeoutSeconds = NexoDefaults.ValidationTimeoutSeconds,
                FailOnNoTests = false,
                TestProjectPatterns = new[] { "*Test*.csproj", "*Tests.csproj" }
            },
            Logging = new LoggingConfiguration
            {
                Level = "Information",
                EnableStructuredLogging = true,
                EnableProgressIndicators = true
            }
        };
    }
}

