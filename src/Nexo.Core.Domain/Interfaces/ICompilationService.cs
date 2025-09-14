using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models;

namespace Nexo.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for compiling code to assemblies
    /// </summary>
    public interface ICompilationService
    {
        /// <summary>
        /// Compiles C# code to a byte array assembly
        /// </summary>
        /// <param name="code">C# source code to compile</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Compilation result with assembly bytes</returns>
        Task<CompilationResult> CompileToAssemblyAsync(string code, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Compiles and fixes compilation errors using AI
        /// </summary>
        /// <param name="code">C# source code to compile</param>
        /// <param name="errors">Compilation errors to fix</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Compilation result with fixed code</returns>
        Task<CompilationResult> FixAndCompileAsync(string code, string[] errors, CancellationToken cancellationToken = default);
    }
}
