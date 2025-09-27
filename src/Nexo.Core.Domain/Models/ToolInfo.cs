using System;

namespace Nexo.Core.Domain.Models
{
    /// <summary>
    /// Information about an available tool
    /// </summary>
    public partial class ToolInfo
    {
        /// <summary>
        /// Name of the tool
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of what the tool does
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Current version of the tool
        /// </summary>
        public int Version { get; set; } = 1;
        
        /// <summary>
        /// When the tool was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the tool was last modified
        /// </summary>
        public DateTime ModifiedAt { get; set; }
        
        /// <summary>
        /// Command name for CLI usage
        /// </summary>
        public string CommandName { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the tool is currently loaded
        /// </summary>
        public bool IsLoaded { get; set; }
    }
}
