using System;

namespace Nexo.Core.Domain.Models
{
    /// <summary>
    /// Represents a saved tool in the repository
    /// </summary>
    public class SavedTool
    {
        /// <summary>
        /// Unique identifier for the tool
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Name of the tool
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the tool
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Version of the tool
        /// </summary>
        public int Version { get; set; } = 1;
        
        /// <summary>
        /// When the tool was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the tool was last modified
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// File path to the assembly
        /// </summary>
        public string AssemblyPath { get; set; } = string.Empty;
        
        /// <summary>
        /// File path to the source code
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;
    }
}
