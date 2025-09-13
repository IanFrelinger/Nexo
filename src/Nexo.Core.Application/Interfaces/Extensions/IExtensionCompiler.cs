using Nexo.Core.Domain.Models.Extensions;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Extensions
{
    /// <summary>
    /// Interface for compiling generated extension code into assemblies.
    /// </summary>
    public interface IExtensionCompiler
    {
        /// <summary>
        /// Compiles generated code into a loadable assembly.
        /// </summary>
        Task<ExtensionGenerationResult> CompileExtensionAsync(string code, string assemblyName);
    }
}