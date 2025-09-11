using System;
using System.Collections.Generic;

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
        /// Programming language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Review type
        /// </summary>
        public string ReviewType { get; set; } = string.Empty;
        
        /// <summary>
        /// Review criteria
        /// </summary>
        public List<string> ReviewCriteria { get; set; } = new();
        
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
