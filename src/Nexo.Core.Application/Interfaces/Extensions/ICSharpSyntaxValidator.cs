using System.Threading.Tasks;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Application.Interfaces.Extensions
{
    /// <summary>
    /// Interface for validating C# syntax in generated extension code
    /// </summary>
    public interface ICSharpSyntaxValidator
    {
        /// <summary>
        /// Validates C# syntax for the provided code
        /// </summary>
        /// <param name="code">The C# code to validate</param>
        /// <returns>Validation result with syntax errors and warnings</returns>
        Task<ValidationResult> ValidateAsync(string code);
    }
}
