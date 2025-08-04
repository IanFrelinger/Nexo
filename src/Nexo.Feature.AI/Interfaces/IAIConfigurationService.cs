using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Service for managing AI-specific configuration settings.
    /// </summary>
    public interface IAIConfigurationService
    {
        /// <summary>
        /// Gets the current AI configuration.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The AI configuration.</returns>
        Task<AIConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the AI configuration.
        /// </summary>
        /// <param name="configuration">The AI configuration to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SaveConfigurationAsync(AIConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads AI configuration for a specific mode.
        /// </summary>
        /// <param name="mode">The AI mode to load configuration for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The AI configuration for the specified mode.</returns>
        Task<AIConfiguration> LoadForModeAsync(AIMode mode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the default AI configuration for a mode.
        /// </summary>
        /// <param name="mode">The AI mode.</param>
        /// <returns>The default AI configuration for the mode.</returns>
        AIConfiguration GetDefaultConfiguration(AIMode mode);

        /// <summary>
        /// Validates the AI configuration.
        /// </summary>
        /// <param name="configuration">The AI configuration to validate.</param>
        /// <returns>Validation result.</returns>
        Task<AIConfigurationValidationResult> ValidateAsync(AIConfiguration configuration);

        /// <summary>
        /// Merges multiple AI configurations.
        /// </summary>
        /// <param name="configurations">The configurations to merge.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The merged AI configuration.</returns>
        Task<AIConfiguration> MergeAsync(IEnumerable<AIConfiguration> configurations, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the configuration path for AI settings.
        /// </summary>
        /// <returns>The AI configuration path.</returns>
        string GetConfigurationPath();

        /// <summary>
        /// Checks if AI configuration exists.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if AI configuration exists; otherwise, false.</returns>
        Task<bool> ExistsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reloads the AI configuration from storage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The reloaded AI configuration.</returns>
        Task<AIConfiguration> ReloadAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of AI configuration validation.
    /// </summary>
    public class AIConfigurationValidationResult
    {
        /// <summary>
        /// Gets or sets whether the configuration is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        public List<AIConfigurationValidationError> Errors { get; set; } = new List<AIConfigurationValidationError>();

        /// <summary>
        /// Gets or sets the validation warnings.
        /// </summary>
        public List<AIConfigurationValidationWarning> Warnings { get; set; } = new List<AIConfigurationValidationWarning>();
    }

    /// <summary>
    /// AI configuration validation error.
    /// </summary>
    public class AIConfigurationValidationError
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
    public class AIConfigurationValidationWarning
    {
        /// <summary>
        /// Gets or sets the warning code.
        /// </summary>
        public string Code { get; set; } = string.Empty;

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
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Warning;
    }

    /// <summary>
    /// Validation severity levels.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Information level.
        /// </summary>
        Information,

        /// <summary>
        /// Warning level.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level.
        /// </summary>
        Error,

        /// <summary>
        /// Critical level.
        /// </summary>
        Critical
    }
} 