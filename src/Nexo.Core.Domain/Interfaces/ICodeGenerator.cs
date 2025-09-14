using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models;

namespace Nexo.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for generating code using AI
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// Generates code from a natural language description
        /// </summary>
        /// <param name="description">Natural language description of the desired tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated code wrapped in plugin interface</returns>
        Task<GeneratedCode> GenerateFromDescriptionAsync(string description, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generates code from a specific prompt
        /// </summary>
        /// <param name="prompt">Specific code generation prompt</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated code</returns>
        Task<GeneratedCode> GenerateFromPromptAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
