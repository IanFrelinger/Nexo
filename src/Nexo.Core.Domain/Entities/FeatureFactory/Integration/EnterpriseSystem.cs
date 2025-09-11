using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory.Integration
{
    /// <summary>
    /// Enterprise system configuration
    /// </summary>
    public class EnterpriseSystem
    {
        /// <summary>
        /// System ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// System name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// System type
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// System version
        /// </summary>
        public string Version { get; set; } = string.Empty;
        
        /// <summary>
        /// System URL
        /// </summary>
        public string? Url { get; set; }
        
        /// <summary>
        /// Authentication type
        /// </summary>
        public string? AuthenticationType { get; set; }
        
        /// <summary>
        /// API endpoints
        /// </summary>
        public List<APIEndpoint> ApiEndpoints { get; set; } = new();
        
        /// <summary>
        /// Database configurations
        /// </summary>
        public List<DatabaseConfiguration> DatabaseConfigurations { get; set; } = new();
        
        /// <summary>
        /// Message queue configurations
        /// </summary>
        public List<MessageQueueConfiguration> MessageQueueConfigurations { get; set; } = new();
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Timestamp when system was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Timestamp when system was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
