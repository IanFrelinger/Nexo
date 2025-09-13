using Nexo.Core.Domain.Models.Extensions;
using Nexo.Core.Domain.Composition;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Extensions
{
    /// <summary>
    /// Interface for generating extensions using AI.
    /// </summary>
    public interface IExtensionGenerator
    {
        /// <summary>
        /// Generates a complete extension from a request.
        /// </summary>
        Task<ExtensionGenerationResult> GenerateExtensionAsync(ExtensionRequest request);

        /// <summary>
        /// Generates only the code without compilation.
        /// </summary>
        Task<string> GenerateCodeAsync(ExtensionRequest request);

        /// <summary>
        /// Validates generated code for syntax errors.
        /// </summary>
        Task<ValidationResult> ValidateCodeAsync(string code);

        /// <summary>
        /// Compiles generated code into an assembly.
        /// </summary>
        Task<ExtensionGenerationResult> CompileCodeAsync(string code, ExtensionRequest request);
    }
}