using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services.Configuration;

/// <summary>
/// Validates coding standard configurations.
/// </summary>
public partial class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public CodingStandardConfigurationValidationResult ValidateConfiguration(CodingStandardConfiguration configuration)
    {
        var result = new CodingStandardConfigurationValidationResult
        {
            IsValid = true
        };

        try
        {
            // Validate basic properties
            if (string.IsNullOrWhiteSpace(configuration.Id))
            {
                result.Errors.Add("Configuration ID is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(configuration.Name))
            {
                result.Errors.Add("Configuration name is required");
                result.IsValid = false;
            }

            // Validate standards
            foreach (var standard in configuration.Standards)
            {
                ValidateStandard(standard, result);
            }

            // Validate global settings
            ValidateGlobalSettings(configuration.GlobalSettings, result);

            // Validate agent settings
            foreach (var agentSetting in configuration.AgentSettings.Values)
            {
                ValidateAgentSettings(agentSetting, result);
            }

            // Validate file type settings
            foreach (var fileTypeSetting in configuration.FileTypeSettings.Values)
            {
                ValidateFileTypeSettings(fileTypeSetting, result);
            }

            // Generate summary
            if (result.IsValid)
            {
                result.Summary = $"Configuration '{configuration.Name}' is valid with {configuration.Standards.Count} standards and {configuration.Standards.Sum(s => s.Rules.Count)} rules";
            }
            else
            {
                result.Summary = $"Configuration '{configuration.Name}' has {result.Errors.Count} errors and {result.Warnings.Count} warnings";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration");
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
            result.Summary = "Configuration validation failed due to an unexpected error";
        }

        return result;
    }

    private void ValidateStandard(CodingStandard standard, CodingStandardConfigurationValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(standard.Id))
        {
            result.Errors.Add($"Standard ID is required for standard '{standard.Name}'");
            result.IsValid = false;
        }

        if (string.IsNullOrWhiteSpace(standard.Name))
        {
            result.Errors.Add("Standard name is required");
            result.IsValid = false;
        }

        if (string.IsNullOrWhiteSpace(standard.Language))
        {
            result.Warnings.Add($"Language not specified for standard '{standard.Name}'");
        }

        // Validate rules
        foreach (var rule in standard.Rules)
        {
            ValidateRule(rule, standard.Name, result);
        }
    }

    private void ValidateRule(CodingStandardRule rule, string standardName, CodingStandardConfigurationValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(rule.Id))
        {
            result.Errors.Add($"Rule ID is required for rule in standard '{standardName}'");
            result.IsValid = false;
        }

        if (string.IsNullOrWhiteSpace(rule.Name))
        {
            result.Errors.Add($"Rule name is required for rule in standard '{standardName}'");
            result.IsValid = false;
        }

        if (string.IsNullOrWhiteSpace(rule.Pattern) && rule.Type == CodingStandardRuleType.Pattern)
        {
            result.Errors.Add($"Pattern is required for pattern rule '{rule.Name}' in standard '{standardName}'");
            result.IsValid = false;
        }

        // Validate regex pattern if it's a pattern rule
        if (rule.Type == CodingStandardRuleType.Pattern && !string.IsNullOrWhiteSpace(rule.Pattern))
        {
            try
            {
                new System.Text.RegularExpressions.Regex(rule.Pattern);
            }
            catch (ArgumentException ex)
            {
                result.Errors.Add($"Invalid regex pattern for rule '{rule.Name}' in standard '{standardName}': {ex.Message}");
                result.IsValid = false;
            }
        }
    }

    private void ValidateGlobalSettings(CodingStandardGlobalSettings settings, CodingStandardConfigurationValidationResult result)
    {
        if (settings.MaxViolationsAllowed < 0)
        {
            result.Errors.Add("MaxViolationsAllowed must be non-negative");
            result.IsValid = false;
        }

        if (settings.MinimumQualityScore < 0 || settings.MinimumQualityScore > 100)
        {
            result.Errors.Add("MinimumQualityScore must be between 0 and 100");
            result.IsValid = false;
        }

        if (settings.ValidationTimeoutMs <= 0)
        {
            result.Errors.Add("ValidationTimeoutMs must be positive");
            result.IsValid = false;
        }
    }

    private void ValidateAgentSettings(CodingStandardAgentSettings settings, CodingStandardConfigurationValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(settings.AgentId))
        {
            result.Errors.Add("Agent ID is required for agent settings");
            result.IsValid = false;
        }
    }

    private void ValidateFileTypeSettings(CodingStandardFileTypeSettings settings, CodingStandardConfigurationValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(settings.FilePattern))
        {
            result.Errors.Add("File pattern is required for file type settings");
            result.IsValid = false;
        }
    }
}
