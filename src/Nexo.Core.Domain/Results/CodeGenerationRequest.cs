using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;

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
        /// Generation prompt
        /// </summary>
        public string Prompt { get; set; } = string.Empty;
        
        /// <summary>
        /// Target platform
        /// </summary>
        public string Platform { get; set; } = string.Empty;
        
        /// <summary>
        /// Programming language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Generation context
        /// </summary>
        public string Context { get; set; } = string.Empty;
        
        /// <summary>
        /// Generation code
        /// </summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Generation framework
        /// </summary>
        public string Framework { get; set; } = string.Empty;
        
        /// <summary>
        /// Generation options
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new();
        
        /// <summary>
        /// Generation requirements
        /// </summary>
        public AIRequirements Requirements { get; set; } = new();
        
        /// <summary>
        /// Generation max tokens
        /// </summary>
        public int MaxTokens { get; set; } = 1000;
        
        /// <summary>
        /// Generation temperature
        /// </summary>
        public double Temperature { get; set; } = 0.7;
        
        /// <summary>
        /// Generation created at
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Generation generated code
        /// </summary>
        public string GeneratedCode { get; set; } = string.Empty;
        
        /// <summary>
        /// Generation explanation
        /// </summary>
        public string Explanation { get; set; } = string.Empty;
        
        /// <summary>
        /// Generation confidence
        /// </summary>
        public AIConfidenceLevel Confidence { get; set; }
        
        /// <summary>
        /// Generation confidence score
        /// </summary>
        public double ConfidenceScore { get; set; }
        
        /// <summary>
        /// Generation suggestions
        /// </summary>
        public List<string> Suggestions { get; set; } = new();
        
        /// <summary>
        /// Generation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Generation metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        /// <summary>
        /// Generation error
        /// </summary>
        public string? Error { get; set; }
        
        /// <summary>
        /// Generation result
        /// </summary>
        public CodeGenerationResult? Result { get; set; }
        
        /// <summary>
        /// Generation completed
        /// </summary>
        public bool GenerationCompleted { get; set; } = false;
        
        /// <summary>
        /// Generation time
        /// </summary>
        public DateTime GenerationTime { get; set; }
        
        /// <summary>
        /// Generation quality score
        /// </summary>
        public int QualityScore { get; set; }
        
        /// <summary>
        /// Generation coverage
        /// </summary>
        public int Coverage { get; set; }
        
        /// <summary>
        /// Generation engine type
        /// </summary>
        public AIEngineType EngineType { get; set; }
        
        /// <summary>
        /// Generation tags
        /// </summary>
        public List<string> Tags { get; set; } = new();
        
        /// <summary>
        /// Generation duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Generation completed at
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Generation success
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Generation success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Generation error message
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Generation exception
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Generation validation errors
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new();
        
        /// <summary>
        /// Generation data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Performance requirements
        /// </summary>
        public Dictionary<string, object> PerformanceRequirements { get; set; } = new();
        
        /// <summary>
        /// Quality requirements
        /// </summary>
        public Dictionary<string, object> QualityRequirements { get; set; } = new();
        
        /// <summary>
        /// Generation additional context
        /// </summary>
        public Dictionary<string, object> AdditionalContext { get; set; } = new();
        
        /// <summary>
        /// Generation additional data
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new();
        
        /// <summary>
        /// Generation timestamp when request was created
        /// </summary>
        public DateTime RequestCreatedAt { get; set; } = DateTime.UtcNow;
    }
}
