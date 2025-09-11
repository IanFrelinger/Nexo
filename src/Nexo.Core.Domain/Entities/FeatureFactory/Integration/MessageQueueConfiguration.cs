using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory.Integration
{
    /// <summary>
    /// Message queue configuration
    /// </summary>
    public class MessageQueueConfiguration
    {
        /// <summary>
        /// Queue ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Queue name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Queue type
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        
        /// <summary>
        /// Server host
        /// </summary>
        public string? Host { get; set; }
        
        /// <summary>
        /// Server port
        /// </summary>
        public int? Port { get; set; }
        
        /// <summary>
        /// Queue name
        /// </summary>
        public string? QueueName { get; set; }
        
        /// <summary>
        /// Username
        /// </summary>
        public string? Username { get; set; }
        
        /// <summary>
        /// Password
        /// </summary>
        public string? Password { get; set; }
        
        /// <summary>
        /// Additional options
        /// </summary>
        public Dictionary<string, string> Options { get; set; } = new();
        
        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Message timeout in seconds
        /// </summary>
        public int MessageTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Maximum retry count
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Timestamp when configuration was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Timestamp when configuration was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
