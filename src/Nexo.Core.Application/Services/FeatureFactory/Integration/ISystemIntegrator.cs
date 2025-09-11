using Nexo.Core.Domain.Entities.FeatureFactory.ApplicationLogic;
using Nexo.Core.Domain.Entities.FeatureFactory;
using Nexo.Core.Domain.Entities.FeatureFactory.Integration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Integration
{
    /// <summary>
    /// Interface for integrating applications with external systems
    /// </summary>
    public interface ISystemIntegrator
    {
        /// <summary>
        /// Integrates an application with REST APIs
        /// </summary>
        Task<IntegrationResult> IntegrateWithAPIAsync(ApplicationLogicResult applicationLogic, APIEndpoint endpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// Integrates an application with databases
        /// </summary>
        Task<IntegrationResult> IntegrateWithDatabaseAsync(ApplicationLogicResult applicationLogic, DatabaseConfiguration config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Integrates an application with message queues
        /// </summary>
        Task<IntegrationResult> IntegrateWithMessageQueueAsync(ApplicationLogicResult applicationLogic, MessageQueueConfiguration config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Integrates an application with enterprise systems
        /// </summary>
        Task<IntegrationResult> IntegrateWithEnterpriseSystemAsync(ApplicationLogicResult applicationLogic, EnterpriseSystem system, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets up real-time data synchronization
        /// </summary>
        Task<IntegrationResult> SetupRealTimeSyncAsync(ApplicationLogicResult applicationLogic, SyncConfiguration config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates integration configuration
        /// </summary>
        Task<ValidationResult> ValidateIntegrationAsync(IntegrationConfiguration config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests integration connectivity
        /// </summary>
        Task<ConnectivityTestResult> TestConnectivityAsync(IntegrationEndpoint endpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets integration status
        /// </summary>
        Task<IntegrationStatus> GetIntegrationStatusAsync(string integrationId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of integration operations
    /// </summary>
    public class IntegrationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string IntegrationId { get; set; } = string.Empty;
        public IntegrationType Type { get; set; } = IntegrationType.API;
        public IntegrationStatus Status { get; set; } = new();
        public List<IntegrationMapping> Mappings { get; set; } = new();
        public List<IntegrationLog> Logs { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of validation operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of connectivity tests
    /// </summary>
    public class ConnectivityTestResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public int StatusCode { get; set; }
        public string ResponseMessage { get; set; } = string.Empty;
        public DateTime TestedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Integration status information
    /// </summary>
    public class IntegrationStatus
    {
        public string Id { get; set; } = string.Empty;
        public IntegrationState State { get; set; } = IntegrationState.Pending;
        public string Message { get; set; } = string.Empty;
        public int Progress { get; set; }
        public List<IntegrationStep> Steps { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Integration log entry
    /// </summary>
    public class IntegrationLog
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public LogLevel Level { get; set; } = LogLevel.Information;
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Integration step information
    /// </summary>
    public class IntegrationStep
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public StepStatus Status { get; set; } = StepStatus.Pending;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Validation error information
    /// </summary>
    public class ValidationError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Validation warning information
    /// </summary>
    public class ValidationWarning
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Enums for integration operations

    /// <summary>
    /// Types of integrations
    /// </summary>
    public enum IntegrationType
    {
        API,
        Database,
        MessageQueue,
        Enterprise,
        RealTimeSync
    }

    /// <summary>
    /// Integration states
    /// </summary>
    public enum IntegrationState
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Disconnected,
        Reconnecting
    }

    /// <summary>
    /// Log levels
    /// </summary>
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Step status
    /// </summary>
    public enum StepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Skipped
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
