using System;

namespace Nexo.Core.Domain.Models
{
    /// <summary>
    /// Represents the result of a compilation operation
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// Whether the compilation was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The compiled assembly as byte array
        /// </summary>
        public byte[]? Assembly { get; set; }
        
        /// <summary>
        /// Compilation errors if any
        /// </summary>
        public string[] Errors { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// Compilation warnings if any
        /// </summary>
        public string[] Warnings { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// The fixed source code if errors were corrected
        /// </summary>
        public string? FixedSourceCode { get; set; }
        
        /// <summary>
        /// Compilation timestamp
        /// </summary>
        public DateTime CompiledAt { get; set; } = DateTime.UtcNow;
    }
}
