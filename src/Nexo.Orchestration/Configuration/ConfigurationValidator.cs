using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Orchestration.Configuration;

/// <summary>
/// Validates orchestration configuration.
/// 
/// Responsibilities:
/// - Validates required settings are present
/// - Validates numeric ranges (retries, timeouts)
/// - Validates connection strings and API keys
/// - Returns validation results with errors and warnings
/// 
/// Used at startup to ensure configuration is valid before orchestration begins.
/// </summary>
public sealed class ConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationValidator>? _logger;

    public ConfigurationValidator(
        IConfiguration configuration,
        ILogger<ConfigurationValidator>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
    }

    /// <summary>
    /// Validates the configuration and returns validation results.
    /// </summary>
    public ConfigurationValidationResult Validate()
    {
        var errors = new List<ConfigurationError>();
        var warnings = new List<ConfigurationWarning>();

        // Validate required settings
        ValidateRequiredSettings(errors);

        // Validate numeric ranges
        ValidateNumericRanges(errors, warnings);

        // Validate connection strings
        ValidateConnectionStrings(errors, warnings);

        var isValid = errors.Count == 0;

        if (!isValid)
        {
            _logger?.LogError("Configuration validation failed with {ErrorCount} errors", errors.Count);
        }
        else if (warnings.Count > 0)
        {
            _logger?.LogWarning("Configuration validation completed with {WarningCount} warnings", warnings.Count);
        }
        else
        {
            _logger?.LogInformation("Configuration validation passed");
        }

        return new ConfigurationValidationResult
        {
            IsValid = isValid,
            Errors = errors,
            Warnings = warnings
        };
    }

    private void ValidateRequiredSettings(List<ConfigurationError> errors)
    {
        var requiredSettings = new[]
        {
            "Nexo:Orchestration:MaxRetries",
            "Nexo:Orchestration:DefaultTimeout"
        };

        foreach (var setting in requiredSettings)
        {
            var value = _configuration[setting];
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new ConfigurationError
                {
                    Setting = setting,
                    Message = $"Required setting '{setting}' is missing or empty"
                });
            }
        }
    }

    private void ValidateNumericRanges(List<ConfigurationError> errors, List<ConfigurationWarning> warnings)
    {
        // Validate max retries
        var maxRetries = _configuration.GetValue<int?>("Nexo:Orchestration:MaxRetries");
        if (maxRetries.HasValue)
        {
            if (maxRetries < 1 || maxRetries > 10)
            {
                errors.Add(new ConfigurationError
                {
                    Setting = "Nexo:Orchestration:MaxRetries",
                    Message = $"MaxRetries must be between 1 and 10, got {maxRetries}"
                });
            }
            else if (maxRetries > 5)
            {
                warnings.Add(new ConfigurationWarning
                {
                    Setting = "Nexo:Orchestration:MaxRetries",
                    Message = $"High MaxRetries value ({maxRetries}) may cause long execution times"
                });
            }
        }

        // Validate timeout
        var timeoutSeconds = _configuration.GetValue<int?>("Nexo:Orchestration:DefaultTimeout");
        if (timeoutSeconds.HasValue)
        {
            if (timeoutSeconds < 1 || timeoutSeconds > 3600)
            {
                errors.Add(new ConfigurationError
                {
                    Setting = "Nexo:Orchestration:DefaultTimeout",
                    Message = $"DefaultTimeout must be between 1 and 3600 seconds, got {timeoutSeconds}"
                });
            }
        }
    }

    private void ValidateConnectionStrings(List<ConfigurationError> errors, List<ConfigurationWarning> warnings)
    {
        // Check for LLM API keys if LLM is enabled
        var useLLM = _configuration.GetValue<bool>("Nexo:Orchestration:UseLLM", false);
        if (useLLM)
        {
            var apiKey = _configuration["Nexo:LLM:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                warnings.Add(new ConfigurationWarning
                {
                    Setting = "Nexo:LLM:ApiKey",
                    Message = "LLM is enabled but API key is not configured"
                });
            }
        }
    }

}
