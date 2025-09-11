using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Requests
{
    /// <summary>
    /// Request for creating a backup
    /// </summary>
    public class BackupRequest
    {
        /// <summary>
        /// Request ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Operation ID to backup
        /// </summary>
        public string OperationId { get; set; } = string.Empty;
        
        /// <summary>
        /// Backup description
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Backup type
        /// </summary>
        public string BackupType { get; set; } = string.Empty;
        
        /// <summary>
        /// Data to backup
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Backup metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        /// <summary>
        /// Timestamp when request was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether the backup is automatic
        /// </summary>
        public bool IsAutomatic { get; set; } = false;
        
        /// <summary>
        /// Backup priority
        /// </summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>
        /// Target path for backup
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether to include metadata
        /// </summary>
        public bool IncludeMetadata { get; set; } = true;
        
        /// <summary>
        /// Whether compression is enabled
        /// </summary>
        public bool CompressionEnabled { get; set; } = true;
        
        /// <summary>
        /// Retention period in days
        /// </summary>
        public int RetentionDays { get; set; } = 30;
    }
}
