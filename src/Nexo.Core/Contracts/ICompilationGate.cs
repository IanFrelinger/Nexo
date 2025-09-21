using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Interface for validating source code compilation.
    /// </summary>
    public interface ICompilationGate
    {
        /// <summary>
        /// Validates the provided source code for compilation issues.
        /// </summary>
        /// <param name="sourceCode">The source code to validate</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A validation result indicating compilation status</returns>
        ValueTask<ValidationResult> ValidateAsync(string sourceCode, CancellationToken ct = default);
    }
}
