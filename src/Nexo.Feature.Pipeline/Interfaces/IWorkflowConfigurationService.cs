using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Service for managing workflow configurations.
    /// </summary>
    public interface IWorkflowConfigurationService
    {
        /// <summary>
        /// Loads a workflow configuration from a file.
        /// </summary>
        /// <param name="filePath">Path to the configuration file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Workflow configuration.</returns>
        Task<WorkflowConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads a workflow configuration from JSON string.
        /// </summary>
        /// <param name="json">JSON configuration string.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Workflow configuration.</returns>
        Task<WorkflowConfiguration> LoadFromJsonAsync(string json, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves a workflow configuration to a file.
        /// </summary>
        /// <param name="configuration">Configuration to save.</param>
        /// <param name="filePath">Path to save the configuration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SaveToFileAsync(WorkflowConfiguration configuration, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the default configuration for a workflow type.
        /// </summary>
        /// <param name="type">Type of workflow.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Default workflow configuration.</returns>
        Task<WorkflowConfiguration> GetDefaultConfigurationAsync(WorkflowType type, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available workflow templates.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of available template names.</returns>
        Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets documentation for a workflow template.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Template documentation.</returns>
        Task<string> GetTemplateDocumentationAsync(string templateName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a workflow configuration.
        /// </summary>
        /// <param name="configuration">Configuration to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result.</returns>
        Task<WorkflowValidationResult> ValidateAsync(WorkflowConfiguration configuration, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of workflow configuration validation.
    /// </summary>
    public class WorkflowValidationResult
    {
        /// <summary>
        /// Whether the configuration is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// List of validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}