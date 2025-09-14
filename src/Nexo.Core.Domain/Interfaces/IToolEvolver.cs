using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models;

namespace Nexo.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for evolving and modifying existing tools
    /// </summary>
    public interface IToolEvolver
    {
        /// <summary>
        /// Evolves an existing tool with modifications
        /// </summary>
        /// <param name="toolName">Name of the tool to evolve</param>
        /// <param name="modification">Description of the modification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Evolved tool information</returns>
        Task<EvolvedTool> EvolveToolAsync(string toolName, string modification, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if a tool can be evolved
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the tool can be evolved</returns>
        Task<bool> CanEvolveToolAsync(string toolName, CancellationToken cancellationToken = default);
    }
}
