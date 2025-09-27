using System;

namespace Nexo.Core.Domain.Models
{
    /// <summary>
    /// Represents generated code with metadata
    /// </summary>
    public partial class GeneratedCode
    {
        /// <summary>
        /// The generated source code
        /// </summary>
        public string SourceCode { get; set; } = string.Empty;
        
        /// <summary>
        /// The tool name extracted from the code
        /// </summary>
        public string ToolName { get; set; } = string.Empty;
        
        /// <summary>
        /// The tool description
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the code is wrapped in a plugin interface
        /// </summary>
        public bool IsWrappedInPlugin { get; set; }
        
        /// <summary>
        /// Generation timestamp
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Any warnings or notes about the generated code
        /// </summary>
        public string[] Warnings { get; set; } = Array.Empty<string>();
    }
}
