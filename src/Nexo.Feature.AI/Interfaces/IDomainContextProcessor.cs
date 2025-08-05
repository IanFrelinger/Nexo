using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Interface for processing domain-specific context and terminology.
    /// </summary>
    public interface IDomainContextProcessor
    {
        /// <summary>
        /// Processes domain-specific language and extracts domain context.
        /// </summary>
        /// <param name="input">The input text to process.</param>
        /// <param name="domain">The target domain (e.g., "E-commerce", "Healthcare", "Finance").</param>
        /// <param name="context">Additional processing context.</param>
        /// <returns>Domain context processing result.</returns>
        Task<DomainContextResult> ProcessDomainContextAsync(string input, string domain, DomainProcessingContext context);

        /// <summary>
        /// Recognizes and extracts business terminology from the input.
        /// </summary>
        /// <param name="input">The input text to analyze.</param>
        /// <param name="domain">The target domain.</param>
        /// <returns>Business terminology recognition result.</returns>
        Task<BusinessTerminologyResult> RecognizeBusinessTerminologyAsync(string input, string domain);

        /// <summary>
        /// Identifies industry-specific requirement patterns in the input.
        /// </summary>
        /// <param name="input">The input text to analyze.</param>
        /// <param name="industry">The target industry.</param>
        /// <returns>Industry pattern recognition result.</returns>
        Task<IndustryPatternResult> IdentifyIndustryPatternsAsync(string input, string industry);

        /// <summary>
        /// Integrates with the domain knowledge base to enhance understanding.
        /// </summary>
        /// <param name="input">The input text to enhance.</param>
        /// <param name="domain">The target domain.</param>
        /// <returns>Enhanced input with domain knowledge integration.</returns>
        Task<DomainKnowledgeResult> IntegrateDomainKnowledgeAsync(string input, string domain);

        /// <summary>
        /// Gets supported domains.
        /// </summary>
        /// <returns>List of supported domains.</returns>
        IEnumerable<string> GetSupportedDomains();

        /// <summary>
        /// Gets supported industries.
        /// </summary>
        /// <returns>List of supported industries.</returns>
        IEnumerable<string> GetSupportedIndustries();

        /// <summary>
        /// Validates domain-specific requirements against domain rules.
        /// </summary>
        /// <param name="requirements">The requirements to validate.</param>
        /// <param name="domain">The target domain.</param>
        /// <returns>Domain validation result.</returns>
        Task<DomainValidationResult> ValidateDomainRequirementsAsync(IEnumerable<FeatureRequirement> requirements, string domain);

        /// <summary>
        /// Suggests domain-specific improvements for requirements.
        /// </summary>
        /// <param name="requirements">The requirements to improve.</param>
        /// <param name="domain">The target domain.</param>
        /// <returns>Domain improvement suggestions.</returns>
        Task<DomainImprovementResult> SuggestDomainImprovementsAsync(IEnumerable<FeatureRequirement> requirements, string domain);
    }
}