using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Nexo.Shared.Models;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Service for managing pipeline configurations from various sources.
    /// </summary>
    public interface IPipelineConfigurationService
    {
        /// <summary>
        /// Loads a pipeline configuration from a file.
        /// </summary>
        /// <param name="filePath">The path to the configuration file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded pipeline configuration.</returns>
        Task<PipelineConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Loads a pipeline configuration from a JSON string.
        /// </summary>
        /// <param name="json">The JSON configuration string.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded pipeline configuration.</returns>
        Task<PipelineConfiguration> LoadFromJsonAsync(string json, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Loads a pipeline configuration from command line arguments.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded pipeline configuration.</returns>
        Task<PipelineConfiguration> LoadFromCommandLineAsync(string[] args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Loads a pipeline configuration from a template.
        /// </summary>
        /// <param name="templateName">The template name.</param>
        /// <param name="parameters">Template parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded pipeline configuration.</returns>
        Task<PipelineConfiguration> LoadFromTemplateAsync(string templateName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Saves a pipeline configuration to a file.
        /// </summary>
        /// <param name="configuration">The pipeline configuration to save.</param>
        /// <param name="filePath">The path to save the configuration file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveToFileAsync(PipelineConfiguration configuration, string filePath, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Validates a pipeline configuration.
        /// </summary>
        /// <param name="configuration">The pipeline configuration to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The validation result.</returns>
        Task<Models.PipelineValidationResult> ValidateAsync(PipelineConfiguration configuration, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Merges multiple pipeline configurations.
        /// </summary>
        /// <param name="configurations">The configurations to merge.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The merged pipeline configuration.</returns>
        Task<PipelineConfiguration> MergeAsync(IEnumerable<PipelineConfiguration> configurations, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the available pipeline templates.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The available template names.</returns>
        Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the template documentation.
        /// </summary>
        /// <param name="templateName">The template name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The template documentation.</returns>
        Task<string> GetTemplateDocumentationAsync(string templateName, CancellationToken cancellationToken = default(CancellationToken));
    }


} 