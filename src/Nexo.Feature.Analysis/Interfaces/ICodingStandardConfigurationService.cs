using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Defines the contract for managing coding standards configurations.
    /// </summary>
    public interface ICodingStandardConfigurationService
    {
        /// <summary>
        /// Gets the current coding standards configuration.
        /// </summary>
        /// <returns>The current configuration</returns>
        CodingStandardConfiguration GetCurrentConfiguration();

        /// <summary>
        /// Updates the coding standards configuration.
        /// </summary>
        /// <param name="configuration">The new configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the update operation</returns>
        Task UpdateConfigurationAsync(CodingStandardConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads configuration from a JSON file.
        /// </summary>
        /// <param name="filePath">The path to the JSON configuration file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The loaded configuration</returns>
        Task<CodingStandardConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves configuration to a JSON file.
        /// </summary>
        /// <param name="configuration">The configuration to save</param>
        /// <param name="filePath">The path to save the configuration file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the save operation</returns>
        Task SaveToFileAsync(CodingStandardConfiguration configuration, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads configuration from a JSON string.
        /// </summary>
        /// <param name="jsonContent">The JSON configuration content</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The loaded configuration</returns>
        Task<CodingStandardConfiguration> LoadFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts configuration to JSON string.
        /// </summary>
        /// <param name="configuration">The configuration to convert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The JSON representation of the configuration</returns>
        Task<string> ToJsonAsync(CodingStandardConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a coding standards configuration.
        /// </summary>
        /// <param name="configuration">The configuration to validate</param>
        /// <returns>Validation result with any errors or warnings</returns>
        CodingStandardConfigurationValidationResult ValidateConfiguration(CodingStandardConfiguration configuration);

        /// <summary>
        /// Gets the default coding standards configuration.
        /// </summary>
        /// <returns>The default configuration</returns>
        CodingStandardConfiguration GetDefaultConfiguration();

        /// <summary>
        /// Gets predefined coding standards configurations.
        /// </summary>
        /// <returns>Dictionary of predefined configurations by name</returns>
        Dictionary<string, CodingStandardConfiguration> GetPredefinedConfigurations();

        /// <summary>
        /// Resets configuration to default values.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the reset operation</returns>
        Task ResetToDefaultAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets configuration history (if supported).
        /// </summary>
        /// <returns>List of previous configurations</returns>
        Task<List<CodingStandardConfiguration>> GetConfigurationHistoryAsync();

        /// <summary>
        /// Restores configuration from history.
        /// </summary>
        /// <param name="configurationId">The ID of the configuration to restore</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the restore operation</returns>
        Task RestoreFromHistoryAsync(string configurationId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the result of validating a coding standards configuration.
    /// </summary>
    public class CodingStandardConfigurationValidationResult
    {
        /// <summary>
        /// Gets or sets whether the configuration is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of validation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of validation suggestions.
        /// </summary>
        public List<string> Suggestions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the summary of the validation.
        /// </summary>
        public string Summary { get; set; } = string.Empty;
    }
}
