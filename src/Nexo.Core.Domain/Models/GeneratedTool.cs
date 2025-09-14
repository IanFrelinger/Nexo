using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.CodeQuality;

namespace Nexo.Core.Domain.Models
{
    /// <summary>
    /// Represents a generated tool that can be executed
    /// </summary>
    public class GeneratedTool
    {
        /// <summary>
        /// The plugin instance
        /// </summary>
        public IPlugin Plugin { get; set; } = null!;
        
        /// <summary>
        /// Name of the tool
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the tool
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Command name for CLI usage
        /// </summary>
        public string CommandName { get; set; } = string.Empty;
        
        /// <summary>
        /// When the tool was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether the tool is ready for execution
        /// </summary>
        public bool IsReady { get; set; }
        
        /// <summary>
        /// Quality score of the generated code
        /// </summary>
        public QualityScore? QualityScore { get; set; }
        
        /// <summary>
        /// Executes the tool with the given arguments
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Execution result</returns>
        public async Task<PluginResult> ExecuteAsync(string[] args)
        {
            if (!IsReady || Plugin == null)
            {
                throw new InvalidOperationException("Tool is not ready for execution");
            }
            
            return await Plugin.ExecuteAsync(args);
        }
    }
}
