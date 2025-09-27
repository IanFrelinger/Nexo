using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Audit execution
    /// </summary>
    public partial class AuditExecution
    {
        public string Id { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
        public ExecutionStatus Status { get; set; }
        public List<AuditStep> Steps { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Audit step
    /// </summary>
    public partial class AuditStep
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
        public bool Success { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Audit event
    /// </summary>
    public partial class AuditEvent
    {
        public string Id { get; set; } = string.Empty;
        public string ExecutionId { get; set; } = string.Empty;
        public AuditEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Context { get; set; } = new();
    }

    /// <summary>
    /// Audit event type
    /// </summary>
    public enum AuditEventType
    {
        ExecutionStarted,
        ExecutionCompleted,
        ExecutionFailed,
        StepStarted,
        StepCompleted,
        StepFailed,
        ProviderDecision,
        PolicyDecision,
        RetryAttempt,
        ApprovalRequested,
        ApprovalGranted,
        ApprovalRejected
    }

    /// <summary>
    /// Execution status
    /// </summary>
    public enum ExecutionStatus
    {
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Audit summary
    /// </summary>
    public partial class AuditSummary
    {
        public string ExecutionId { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public ExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int StepCount { get; set; }
        public Dictionary<string, TimeSpan> StepTimings { get; set; } = new();
        public int RetryCount { get; set; }
        public int PolicyDecisions { get; set; }
        public int ProviderDecisions { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Interface for audit service
    /// </summary>
    public interface IAuditService
    {
        Task<string> StartExecutionAsync(
            string executionId,
            string operationType,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default);

        Task AddStepAsync(
            string executionId,
            string stepName,
            string stepType,
            Dictionary<string, object>? stepContext = null,
            CancellationToken cancellationToken = default);

        Task CompleteStepAsync(
            string executionId,
            string stepName,
            bool success,
            Dictionary<string, object>? stepMetrics = null,
            string? errorMessage = null,
            CancellationToken cancellationToken = default);

        Task AddProviderDecisionAsync(
            string executionId,
            string providerName,
            string decision,
            string reasoning,
            Dictionary<string, object>? context = null,
            CancellationToken cancellationToken = default);

        Task AddPolicyDecisionAsync(
            string executionId,
            string policyId,
            string decision,
            string reason,
            bool requiresApproval,
            Dictionary<string, object>? context = null,
            CancellationToken cancellationToken = default);

        Task AddRetryAttemptAsync(
            string executionId,
            string operation,
            int attemptNumber,
            string errorMessage,
            TimeSpan delay,
            CancellationToken cancellationToken = default);

        Task CompleteExecutionAsync(
            string executionId,
            bool success,
            Dictionary<string, object>? finalMetrics = null,
            string? errorMessage = null,
            CancellationToken cancellationToken = default);

        Task<AuditExecution> GetExecutionAsync(
            string executionId,
            CancellationToken cancellationToken = default);

        Task<List<AuditEvent>> GetExecutionEventsAsync(
            string executionId,
            CancellationToken cancellationToken = default);

        Task<AuditSummary> GetExecutionSummaryAsync(
            string executionId,
            CancellationToken cancellationToken = default);
    }
}
