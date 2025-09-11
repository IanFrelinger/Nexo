using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Code generation request
    /// </summary>
    public class CodeGenerationRequest
    {
        /// <summary>
        /// Request ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of what to generate
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Target platform
        /// </summary>
        public string Platform { get; set; } = string.Empty;
        
        /// <summary>
        /// Programming language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Performance requirements
        /// </summary>
        public Dictionary<string, object> PerformanceRequirements { get; set; } = new();
        
        /// <summary>
        /// Quality requirements
        /// </summary>
        public Dictionary<string, object> QualityRequirements { get; set; } = new();
        
        /// <summary>
        /// Additional context
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Timestamp when request was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
