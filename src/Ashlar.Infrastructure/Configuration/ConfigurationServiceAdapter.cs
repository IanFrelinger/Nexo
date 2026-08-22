using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Configuration.Models;
using Ashlar.Core.Application.Configuration.Ports;
using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Exceptions;

namespace Ashlar.Infrastructure.Configuration;

/// <summary>
/// Infrastructure adapter that implements the <see cref="IConfigurationService"/> port
/// from the Application layer (hexagonal architecture boundary).
///
/// <para><b>Config file resolution:</b> The file path is resolved once at construction
/// time using the precedence chain: <c>ASHLAR_CONFIG_PATH</c> env var → default path
/// <c>~/.ashlar/config.json</c> (directory and filename constants live in
/// <see cref="AshlarDefaults"/>). This means each adapter instance is bound to a
/// single file for its lifetime.</para>
///
/// <para><b>Strict mode (<c>failOnConfigWarnings</c>):</b> When enabled,
/// missing or unparseable config files cause a <see cref="ConfigurationException"/>
/// with a descriptive <see cref="ErrorCodes"/> code instead of silently falling back
/// to defaults. CI pipelines and the API host typically enable strict mode to catch
/// misconfigurations early.</para>
///
/// <para><b>Error handling:</b> All failure paths surface a
/// <see cref="ConfigurationException"/> carrying an <see cref="ErrorCodes"/> value,
/// an inner exception where applicable, and an optional remediation hint. Callers
/// can pattern-match on <c>ErrorCode</c> to decide whether to retry or abort.</para>
/// </summary>
public class ConfigurationServiceAdapter : IConfigurationService
{
    private readonly ILogger<ConfigurationServiceAdapter> _logger;
    private readonly string _configFilePath;
    private readonly bool _failOnWarnings;

    /// <summary>
    /// Initializes the adapter and resolves the configuration file path.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="failOnConfigWarnings">
    /// When <c>true</c>, missing or invalid config files throw instead of returning
    /// defaults. Corresponds to <c>AshlarHostingOptions.StrictMode</c> in the hosting layer.
    /// </param>
    public ConfigurationServiceAdapter(ILogger<ConfigurationServiceAdapter> logger, bool failOnConfigWarnings = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _failOnWarnings = failOnConfigWarnings;
        var configPath = Environment.GetEnvironmentVariable("ASHLAR_CONFIG_PATH");
        _configFilePath = !string.IsNullOrWhiteSpace(configPath)
            ? configPath
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                AshlarDefaults.ConfigDirectoryName,
                AshlarDefaults.ConfigFileName);
    }

    /// <summary>
    /// Loads and deserializes the JSON configuration file. In non-strict mode, a
    /// missing or empty file silently returns <see cref="GetDefault"/>. JSON syntax
    /// errors always throw a <see cref="ConfigurationException"/> with
    /// <see cref="ErrorCodes.ConfigInvalidFormat"/> because partial/corrupt JSON
    /// cannot be safely defaulted.
    /// </summary>
    public async Task<AshlarConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configFilePath))
        {
            if (_failOnWarnings)
                throw new ConfigurationException(
                    $"Configuration file not found at {_configFilePath} (strict mode is enabled — create the file or set ASHLAR_CONFIG_PATH).",
                    ErrorCodes.ConfigFileNotFound);
            _logger.LogInformation("Configuration file not found, using defaults");
            return GetDefault();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            var config = JsonSerializer.Deserialize<AshlarConfiguration>(json, new JsonSerializerOptions
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

    /// <summary>
    /// Serializes and persists the configuration to the resolved file path.
    /// The parent directory is created on demand so that first-run scenarios
    /// (fresh <c>~/.ashlar/</c>) work without manual setup. Failures are wrapped
    /// in <see cref="ConfigurationException"/> with
    /// <see cref="ErrorCodes.ConfigInvalidValue"/>.
    /// </summary>
    public async Task SaveAsync(AshlarConfiguration configuration, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Returns a <see cref="AshlarConfiguration"/> populated with safe production
    /// defaults. Threshold values (e.g. <c>MaxComplexityThreshold</c>,
    /// <c>TimeoutSeconds</c>) are sourced from <see cref="AshlarDefaults"/> so they
    /// stay in sync with documentation and other consumers.
    /// </summary>
    public AshlarConfiguration GetDefault()
    {
        return new AshlarConfiguration
        {
            Analysis = new AnalysisConfiguration
            {
                EnabledRules = new[] { "SecurityScan", "CodeQuality" },
                MaxComplexityThreshold = AshlarDefaults.AnalysisMaxComplexityThreshold,
                EnableSecurityScan = true,
                EnableCodeQuality = true
            },
            Validation = new ValidationConfiguration
            {
                TimeoutSeconds = AshlarDefaults.ValidationTimeoutSeconds,
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

