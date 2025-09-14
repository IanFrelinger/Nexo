using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models;

namespace Nexo.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for persisting and managing generated tools
    /// </summary>
    public interface IToolRepository
    {
        /// <summary>
        /// Saves a generated tool to persistent storage
        /// </summary>
        /// <param name="plugin">The plugin instance</param>
        /// <param name="assembly">Compiled assembly bytes</param>
        /// <param name="sourceCode">Original source code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Saved tool information</returns>
        Task<SavedTool> SaveToolAsync(IPlugin plugin, byte[] assembly, string sourceCode, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Lists all available tools
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of tool information</returns>
        Task<IEnumerable<ToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a specific tool by name
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tool information or null if not found</returns>
        Task<ToolInfo?> GetToolAsync(string toolName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Loads a tool's source code
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Source code or null if not found</returns>
        Task<string?> LoadToolSourceAsync(string toolName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Saves a new version of an existing tool
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="sourceCode">New source code</param>
        /// <param name="version">Version number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the save operation</returns>
        Task SaveVersionAsync(string toolName, string sourceCode, int version, CancellationToken cancellationToken = default);
    }
}
