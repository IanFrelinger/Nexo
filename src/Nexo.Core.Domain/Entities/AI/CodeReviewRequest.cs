using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Results;

namespace Nexo.Core.Domain.Entities.AI
{
    /// <summary>
    /// Code review request
    /// </summary>
    public class CodeReviewRequest
    {
        /// <summary>
        /// Request ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Code to review
        /// </summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Review prompt
        /// </summary>
        public string Prompt { get; set; } = string.Empty;
        
        /// <summary>
        /// Programming language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Review type
        /// </summary>
        public string ReviewType { get; set; } = string.Empty;
        
        /// <summary>
        /// Review context
        /// </summary>
        public string Context { get; set; } = string.Empty;
        
        /// <summary>
        /// Review criteria
        /// </summary>
        public List<string> ReviewCriteria { get; set; } = new();
        
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
        
        // Properties accessed by application layer
        public CodeReviewResult? Result { get; set; }
        public bool ReviewCompleted { get; set; } = false;
        public DateTime ReviewTime { get; set; }
    }
}
