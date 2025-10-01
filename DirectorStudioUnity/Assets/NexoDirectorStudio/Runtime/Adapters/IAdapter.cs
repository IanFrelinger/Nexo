using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Base interface for all offline adapters.
    /// Provides common functionality for health checks and service management.
    /// </summary>
    public interface IAdapter
    {
        /// <summary>
        /// Unique identifier for this adapter.
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Human-readable name of the adapter.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Description of what this adapter does.
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Whether this adapter is currently available.
        /// </summary>
        bool IsAvailable { get; }
        
        /// <summary>
        /// Last health check timestamp.
        /// </summary>
        DateTime LastHealthCheck { get; }
        
        /// <summary>
        /// Performs a health check on the adapter.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        Task<HealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Initializes the adapter.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Initialization result</returns>
        Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Disposes of the adapter and cleans up resources.
        /// </summary>
        void Dispose();
    }
    
    /// <summary>
    /// Result of a health check operation.
    /// </summary>
    public sealed record HealthCheckResult
    {
        /// <summary>
        /// Whether the health check passed.
        /// </summary>
        public bool IsHealthy { get; init; }
        
        /// <summary>
        /// Health check message.
        /// </summary>
        public string Message { get; init; } = "";
        
        /// <summary>
        /// Response time in milliseconds.
        /// </summary>
        public long ResponseTimeMs { get; init; }
        
        /// <summary>
        /// Additional health check details.
        /// </summary>
        public string Details { get; init; } = "";
        
        /// <summary>
        /// Timestamp of the health check.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Result of an initialization operation.
    /// </summary>
    public sealed record InitializationResult
    {
        /// <summary>
        /// Whether initialization was successful.
        /// </summary>
        public bool IsSuccessful { get; init; }
        
        /// <summary>
        /// Initialization message.
        /// </summary>
        public string Message { get; init; } = "";
        
        /// <summary>
        /// Initialization details.
        /// </summary>
        public string Details { get; init; } = "";
        
        /// <summary>
        /// Timestamp of the initialization.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
