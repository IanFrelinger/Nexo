using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Template.Interfaces
{
    /// <summary>
    /// Base interface for template services that provide template management and generation capabilities.
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Gets a template by name.
        /// </summary>
        /// <param name="templateName">The name of the template.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The template content.</returns>
        Task<string> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves a template with the specified name.
        /// </summary>
        /// <param name="templateName">The name of the template.</param>
        /// <param name="content">The template content.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SaveTemplateAsync(string templateName, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available template names.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of available template names.</returns>
        Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a template by name.
        /// </summary>
        /// <param name="templateName">The name of the template to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteTemplateAsync(string templateName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a template.
        /// </summary>
        /// <param name="templateName">The name of the template to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the template is valid, false otherwise.</returns>
        Task<bool> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default);
    }
} 