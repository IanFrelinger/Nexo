using Nexo.Core.Domain.Composition;
using Nexo.Core.Domain.Models.Extensions;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Extensions
{
    /// <summary>
    /// Interface for validating C# syntax in generated code.
    /// </summary>
    public interface ICSharpSyntaxValidator
    {
        /// <summary>
        /// Validates C# code for syntax errors and warnings.
        /// </summary>
        Task<ExtensionGenerationResult> ValidateSyntaxAsync(string code, string assemblyName);
    }
}