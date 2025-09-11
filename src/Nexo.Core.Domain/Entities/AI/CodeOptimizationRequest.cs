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
        /// Optimization prompt
        /// </summary>
        public string Prompt { get; set; } = string.Empty;
        
        /// <summary>
        /// Programming language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Optimization type
        /// </summary>
        public string OptimizationType { get; set; } = string.Empty;
        
        /// <summary>
        /// Optimization context
        /// </summary>
        public string Context { get; set; } = string.Empty;
        
        /// <summary>
        /// Optimization level
        /// </summary>
        public string OptimizationLevel { get; set; } = "Medium";
        
        /// <summary>
        /// Optimization result
        /// </summary>
        public CodeOptimizationResult? Result { get; set; }
        
        /// <summary>
        /// Whether optimization is completed
        /// </summary>
        public bool OptimizationCompleted { get; set; } = false;
        
        /// <summary>
        /// Time taken for optimization
        /// </summary>
        public TimeSpan OptimizationTime { get; set; }
        
        /// <summary>
        /// Performance requirements
        /// </summary>
        public Dictionary<string, object> PerformanceRequirements { get; set; } = new();
        
        /// <summary>
        /// Quality requirements
        /// </summary>
        public Dictionary<string, object> QualityRequirements { get; set; } = new();
        
        /// <summary>
        /// Additional context data
        /// </summary>
        public Dictionary<string, object> AdditionalContext { get; set; } = new();
        
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
