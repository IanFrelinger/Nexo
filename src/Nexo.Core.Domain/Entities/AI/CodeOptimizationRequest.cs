using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.AI
{
    /// <summary>
    /// Code optimization request
    /// </summary>
    public class CodeOptimizationRequest
    {
        /// <summary>
        /// Request ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Code to optimize
        /// </summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Programming language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Optimization type
        /// </summary>
        public string OptimizationType { get; set; } = string.Empty;
        
        /// <summary>
        /// Optimization level
        /// </summary>
        public string OptimizationLevel { get; set; } = "Medium";
        
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
