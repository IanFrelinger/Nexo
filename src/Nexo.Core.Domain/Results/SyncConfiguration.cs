using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Synchronization configuration
    /// </summary>
    public class SyncConfiguration
    {
        /// <summary>
        /// Configuration ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Configuration name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Source system
        /// </summary>
        public string SourceSystem { get; set; } = string.Empty;
        
        /// <summary>
        /// Target system
        /// </summary>
        public string TargetSystem { get; set; } = string.Empty;
        
        /// <summary>
        /// Sync type
        /// </summary>
        public string SyncType { get; set; } = string.Empty;
        
        /// <summary>
        /// Sync frequency in minutes
        /// </summary>
        public int SyncFrequencyMinutes { get; set; } = 60;
        
        /// <summary>
        /// Sync timeout in seconds
        /// </summary>
        public int SyncTimeoutSeconds { get; set; } = 300;
        
        /// <summary>
        /// Maximum retry count
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
        
        /// <summary>
        /// Sync filters
        /// </summary>
        public Dictionary<string, object> Filters { get; set; } = new();
        
        /// <summary>
        /// Sync mappings
        /// </summary>
        public Dictionary<string, string> Mappings { get; set; } = new();
        
        /// <summary>
        /// Additional options
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new();
        
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
