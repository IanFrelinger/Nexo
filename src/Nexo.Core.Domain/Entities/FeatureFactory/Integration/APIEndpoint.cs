using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory.Integration
{
    /// <summary>
    /// API endpoint configuration
    /// </summary>
    public class APIEndpoint
    {
        /// <summary>
        /// Endpoint ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Endpoint name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Endpoint URL
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// HTTP method
        /// </summary>
        public string Method { get; set; } = "GET";
        
        /// <summary>
        /// Endpoint description
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Authentication type
        /// </summary>
        public string? AuthenticationType { get; set; }
        
        /// <summary>
        /// Headers
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();
        
        /// <summary>
        /// Query parameters
        /// </summary>
        public Dictionary<string, string> QueryParameters { get; set; } = new();
        
        /// <summary>
        /// Request body
        /// </summary>
        public string? RequestBody { get; set; }
        
        /// <summary>
        /// Response format
        /// </summary>
        public string? ResponseFormat { get; set; }
        
        /// <summary>
        /// Timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Retry count
        /// </summary>
        public int RetryCount { get; set; } = 3;
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Timestamp when endpoint was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Timestamp when endpoint was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
