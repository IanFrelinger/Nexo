using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Template.Interfaces
{
    /// <summary>
    /// Interface for intelligent template services that provide AI-powered template generation and adaptation.
    /// </summary>
    public interface IIntelligentTemplateService : ITemplateService
    {
        /// <summary>
        /// Generates a template based on a description and parameters.
        /// </summary>
        /// <param name="description">Description of the desired template.</param>
        /// <param name="parameters">Optional parameters for template generation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated template content.</returns>
        Task<string> GenerateTemplateAsync(string description, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adapts an existing template based on new requirements.
        /// </summary>
        /// <param name="template">The template to adapt.</param>
        /// <param name="requirements">The new requirements for the template.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The adapted template content.</returns>
        Task<string> AdaptTemplateAsync(string template, IDictionary<string, object> requirements, CancellationToken cancellationToken = default);

        /// <summary>
        /// Suggests improvements for an existing template.
        /// </summary>
        /// <param name="template">The template to analyze.</param>
        /// <param name="context">Optional context for the analysis.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of improvement suggestions.</returns>
        Task<IList<string>> SuggestTemplateImprovementsAsync(string template, IDictionary<string, object> context = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a complete project structure.
        /// </summary>
        /// <param name="projectType">The type of project to generate structure for.</param>
        /// <param name="requirements">Project requirements.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated project structure.</returns>
        Task<string> GenerateProjectStructureAsync(string projectType, IDictionary<string, object> requirements, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a configuration template.
        /// </summary>
        /// <param name="configurationType">The type of configuration to generate.</param>
        /// <param name="settings">Configuration settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated configuration template.</returns>
        Task<string> GenerateConfigurationTemplateAsync(string configurationType, IDictionary<string, object> settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a documentation template.
        /// </summary>
        /// <param name="documentationType">The type of documentation to generate.</param>
        /// <param name="context">Documentation context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated documentation template.</returns>
        Task<string> GenerateDocumentationTemplateAsync(string documentationType, IDictionary<string, object> context, CancellationToken cancellationToken = default);
    }
} 