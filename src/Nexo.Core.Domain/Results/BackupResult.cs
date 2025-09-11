using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Result of backup operations
    /// </summary>
    public class BackupResult
    {
        /// <summary>
        /// Unique identifier for the backup result
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Whether the backup was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Error message if backup failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception if backup failed
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Backup file path
        /// </summary>
        public string? BackupPath { get; set; }
        
        /// <summary>
        /// Backup size in bytes
        /// </summary>
        public long BackupSize { get; set; }
        
        /// <summary>
        /// Backup duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Number of files backed up
        /// </summary>
        public int FilesBackedUp { get; set; }
        
        /// <summary>
        /// Backup metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        /// <summary>
        /// Warnings from the backup
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Timestamp when backup completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static BackupResult Success(string? message = null)
        {
            return new BackupResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static BackupResult Failure(string errorMessage, Exception? exception = null)
        {
            return new BackupResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
        
        /// <summary>
        /// Adds warning
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        /// <summary>
        /// Adds metadata
        /// </summary>
        public void AddMetadata(string key, object value)
        {
            Metadata[key] = value;
        }
    }
}
