using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Infrastructure.DataStore
{
    /// <summary>
    /// Data store operation
    /// </summary>
    public record DataStoreOperation
    {
        /// <summary>
        /// Operation identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Operation type
        /// </summary>
        public string Type { get; init; } = string.Empty;
        
        /// <summary>
        /// Operation parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; init; } = new();
        
        /// <summary>
        /// Operation metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
        
        /// <summary>
        /// Operation timestamp
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Data store operation result
    /// </summary>
    public record DataStoreOperationResult
    {
        /// <summary>
        /// Operation identifier
        /// </summary>
        public string OperationId { get; init; } = string.Empty;
        
        /// <summary>
        /// Operation success
        /// </summary>
        public bool Success { get; init; }
        
        /// <summary>
        /// Result data
        /// </summary>
        public object Data { get; init; }
        
        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; init; } = string.Empty;
        
        /// <summary>
        /// Execution time
        /// </summary>
        public TimeSpan ExecutionTime { get; init; }
        
        /// <summary>
        /// Result metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Data store transaction
    /// </summary>
    public record DataStoreTransaction
    {
        /// <summary>
        /// Transaction identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Transaction operations
        /// </summary>
        public List<DataStoreOperation> Operations { get; init; } = new();
        
        /// <summary>
        /// Transaction status
        /// </summary>
        public string Status { get; init; } = "Pending";
        
        /// <summary>
        /// Transaction metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
        
        /// <summary>
        /// Transaction timestamp
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Data store query
    /// </summary>
    public record DataStoreQuery
    {
        /// <summary>
        /// Query identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Query text
        /// </summary>
        public string QueryText { get; init; } = string.Empty;
        
        /// <summary>
        /// Query parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; init; } = new();
        
        /// <summary>
        /// Query timeout
        /// </summary>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Query metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Data store query result
    /// </summary>
    public record DataStoreQueryResult
    {
        /// <summary>
        /// Query identifier
        /// </summary>
        public string QueryId { get; init; } = string.Empty;
        
        /// <summary>
        /// Result data
        /// </summary>
        public List<Dictionary<string, object>> Data { get; init; } = new();
        
        /// <summary>
        /// Result count
        /// </summary>
        public int Count { get; init; }
        
        /// <summary>
        /// Execution time
        /// </summary>
        public TimeSpan ExecutionTime { get; init; }
        
        /// <summary>
        /// Result metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Data store batch operation
    /// </summary>
    public record DataStoreBatchOperation
    {
        /// <summary>
        /// Batch identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Batch operations
        /// </summary>
        public List<DataStoreOperation> Operations { get; init; } = new();
        
        /// <summary>
        /// Batch size
        /// </summary>
        public int BatchSize { get; init; }
        
        /// <summary>
        /// Batch timeout
        /// </summary>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
        
        /// <summary>
        /// Batch metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Data store batch result
    /// </summary>
    public record DataStoreBatchResult
    {
        /// <summary>
        /// Batch identifier
        /// </summary>
        public string BatchId { get; init; } = string.Empty;
        
        /// <summary>
        /// Batch results
        /// </summary>
        public List<DataStoreOperationResult> Results { get; init; } = new();
        
        /// <summary>
        /// Success count
        /// </summary>
        public int SuccessCount { get; init; }
        
        /// <summary>
        /// Failure count
        /// </summary>
        public int FailureCount { get; init; }
        
        /// <summary>
        /// Total execution time
        /// </summary>
        public TimeSpan TotalExecutionTime { get; init; }
        
        /// <summary>
        /// Batch metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
