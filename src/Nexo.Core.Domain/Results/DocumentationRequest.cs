using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Documentation request
    /// </summary>
    public class DocumentationRequest
    {
        /// <summary>
        /// Request ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Code to document
        /// </summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Programming language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Documentation type
        /// </summary>
        public string DocumentationType { get; set; } = string.Empty;
        
        /// <summary>
        /// Documentation criteria
        /// </summary>
        public List<string> DocumentationCriteria { get; set; } = new();
        
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
