using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Service for managing AI-specific configuration settings.
    /// </summary>
    public interface IAiConfigurationService
    {
        /// <summary>
        /// Gets the current AI configuration.
        /// </summary>
        /// <returns>The AI configuration.</returns>
        Task<AiConfiguration> GetConfigurationAsync();

        /// <summary>
        /// Saves the AI configuration.
        /// </summary>
        /// <param name="configuration">The AI configuration to save.</param>
        Task SaveConfigurationAsync(AiConfiguration configuration);

        /// <summary>
        /// Loads AI configuration for a specific mode.
        /// </summary>
        /// <param name="mode">The AI mode to load configuration for.</param>
        /// <returns>The AI configuration for the specified mode.</returns>
        Task<AiConfiguration> LoadForModeAsync(AiMode mode);

        /// <summary>
        /// Gets the default AI configuration for a mode.
        /// </summary>
        /// <param name="mode">The AI mode.</param>
        /// <returns>The default AI configuration for the mode.</returns>
        AiConfiguration GetDefaultConfiguration(AiMode mode);

        /// <summary>
        /// Validates the AI configuration.
        /// </summary>
        /// <param name="configuration">The AI configuration to validate.</param>
        /// <returns>Validation result.</returns>
        Task<AiConfigurationValidationResult> ValidateAsync(AiConfiguration configuration);

        /// <summary>
        /// Merges multiple AI configurations.
        /// </summary>
        /// <param name="configurations">The configurations to merge.</param>
        /// <returns>The merged AI configuration.</returns>
        Task<AiConfiguration> MergeAsync(IEnumerable<AiConfiguration> configurations);

        /// <summary>
        /// Gets the configuration path for AI settings.
        /// </summary>
        /// <returns>The AI configuration path.</returns>
        string GetConfigurationPath();

        /// <summary>
        /// Checks if AI configuration exists.
        /// </summary>
        /// <returns>True if AI configuration exists; otherwise, false.</returns>
        Task<bool> ExistsAsync();

        /// <summary>
        /// Reloads the AI configuration from storage.
        /// </summary>
        /// <returns>The reloaded AI configuration.</returns>
        Task<AiConfiguration> ReloadAsync();
    }

    /// <summary>
    /// Result of AI configuration validation.
    /// </summary>
    public class AiConfigurationValidationResult
    {
        private readonly List<AiConfigurationValidationWarning> _warnings = [];

        /// <summary>
        /// Gets or sets whether the configuration is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        public List<AiConfigurationValidationError> Errors { get; set; } = [];

        /// <summary>
        /// Gets or sets the validation warnings.
        /// </summary>
        public List<AiConfigurationValidationWarning> Warnings => _warnings;
    }

    /// <summary>
    /// AI configuration validation error.
    /// </summary>
    public class AiConfigurationValidationError
    {
        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the field path.
        /// </summary>
        public string FieldPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    }

    /// <summary>
    /// AI configuration validation warning.
    /// </summary>
    public class AiConfigurationValidationWarning
    {
        public AiConfigurationValidationWarning(ValidationSeverity severity)
        {
            Severity = severity;
        }

        /// <summary>
        /// Gets or sets the warning message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the field path.
        /// </summary>
        public string FieldPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        public ValidationSeverity Severity { get; set; }
    }

    /// <summary>
    /// Validation severity levels.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Error level.
        /// </summary>
        Error
    }
} 