using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Enhanced audit service with detailed logging and metrics
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> _logger;
        private readonly Dictionary<string, AuditExecution> _executions;
        private readonly List<AuditEvent> _events;

        public AuditService(ILogger<AuditService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _executions = new Dictionary<string, AuditExecution>();
            _events = new List<AuditEvent>();
        }

        public async Task<string> StartExecutionAsync(
            string executionId,
            string operationType,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default)
        {
            var execution = new AuditExecution
            {
                Id = executionId,
                OperationType = operationType,
                Context = context,
                StartTime = DateTime.UtcNow,
                Status = ExecutionStatus.Running,
                Steps = new List<AuditStep>(),
                Metrics = new Dictionary<string, object>()
            };

            _executions[executionId] = execution;

            var event_ = new AuditEvent
            {
                Id = Guid.NewGuid().ToString(),
                ExecutionId = executionId,
                EventType = AuditEventType.ExecutionStarted,
                Timestamp = DateTime.UtcNow,
                Message = $"Execution {executionId} started",
                Context = context
            };

            _events.Add(event_);

            _logger.LogInformation("Started execution {ExecutionId} of type {OperationType}", executionId, operationType);

            return await Task.FromResult(executionId);
        }

        public async Task AddStepAsync(
            string executionId,
            string stepName,
            string stepType,
            Dictionary<string, object>? stepContext = null,
            CancellationToken cancellationToken = default)
        {
            if (!_executions.TryGetValue(executionId, out var execution))
            {
                _logger.LogWarning("Execution {ExecutionId} not found for step {StepName}", executionId, stepName);
                return;
            }

            var step = new AuditStep
            {
                Name = stepName,
                Type = stepType,
                StartTime = DateTime.UtcNow,
                Context = stepContext ?? new Dictionary<string, object>(),
                Metrics = new Dictionary<string, object>()
            };

            execution.Steps.Add(step);

            var event_ = new AuditEvent
            {
                Id = Guid.NewGuid().ToString(),
                ExecutionId = executionId,
                EventType = AuditEventType.StepStarted,
                Timestamp = DateTime.UtcNow,
                Message = $"Step {stepName} started",
                Context = stepContext ?? new Dictionary<string, object>()
            };

            _events.Add(event_);

            _logger.LogDebug("Added step {StepName} to execution {ExecutionId}", stepName, executionId);
        }

        public async Task CompleteStepAsync(
            string executionId,
            string stepName,
            bool success,
            Dictionary<string, object>? stepMetrics = null,
            string? errorMessage = null,
            CancellationToken cancellationToken = default)
        {
            if (!_executions.TryGetValue(executionId, out var execution))
            {
                _logger.LogWarning("Execution {ExecutionId} not found for step completion {StepName}", executionId, stepName);
                return;
            }

            var step = execution.Steps.FirstOrDefault(s => s.Name == stepName);
            if (step == null)
            {
                _logger.LogWarning("Step {StepName} not found in execution {ExecutionId}", stepName, executionId);
                return;
            }

            step.EndTime = DateTime.UtcNow;
            step.Duration = step.EndTime - step.StartTime;
            step.Success = success;
            step.ErrorMessage = errorMessage;

            if (stepMetrics != null)
            {
                foreach (var metric in stepMetrics)
                {
                    step.Metrics[metric.Key] = metric.Value;
                }
            }

            var event_ = new AuditEvent
            {
                Id = Guid.NewGuid().ToString(),
                ExecutionId = executionId,
                EventType = success ? AuditEventType.StepCompleted : AuditEventType.StepFailed,
                Timestamp = DateTime.UtcNow,
                Message = success ? $"Step {stepName} completed" : $"Step {stepName} failed: {errorMessage}",
                Context = stepMetrics ?? new Dictionary<string, object>()
            };

            _events.Add(event_);

            _logger.LogDebug("Completed step {StepName} in execution {ExecutionId}: Success={Success}, Duration={Duration}ms", 
                stepName, executionId, success, step.Duration.TotalMilliseconds);
        }

        public async Task AddProviderDecisionAsync(
            string executionId,
            string providerName,
            string decision,
            string reasoning,
            Dictionary<string, object>? context = null,
            CancellationToken cancellationToken = default)
        {
            var event_ = new AuditEvent
            {
                Id = Guid.NewGuid().ToString(),
                ExecutionId = executionId,
                EventType = AuditEventType.ProviderDecision,
                Timestamp = DateTime.UtcNow,
                Message = $"Provider {providerName} decision: {decision}",
                Context = new Dictionary<string, object>
                {
                    ["provider"] = providerName,
                    ["decision"] = decision,
                    ["reasoning"] = reasoning
                }.Concat(context ?? new Dictionary<string, object>())
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            _events.Add(event_);

            _logger.LogInformation("Provider {ProviderName} decision for execution {ExecutionId}: {Decision} - {Reasoning}", 
                providerName, executionId, decision, reasoning);
        }

        public async Task AddPolicyDecisionAsync(
            string executionId,
            string policyId,
            string decision,
            string reason,
            bool requiresApproval,
            Dictionary<string, object>? context = null,
            CancellationToken cancellationToken = default)
        {
            var event_ = new AuditEvent
            {
                Id = Guid.NewGuid().ToString(),
                ExecutionId = executionId,
                EventType = AuditEventType.PolicyDecision,
                Timestamp = DateTime.UtcNow,
                Message = $"Policy {policyId} decision: {decision}",
                Context = new Dictionary<string, object>
                {
                    ["policyId"] = policyId,
                    ["decision"] = decision,
                    ["reason"] = reason,
                    ["requiresApproval"] = requiresApproval
                }.Concat(context ?? new Dictionary<string, object>())
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            _events.Add(event_);

            _logger.LogInformation("Policy {PolicyId} decision for execution {ExecutionId}: {Decision} - {Reason} (RequiresApproval: {RequiresApproval})", 
                policyId, executionId, decision, reason, requiresApproval);
        }

        public async Task AddRetryAttemptAsync(
            string executionId,
            string operation,
            int attemptNumber,
            string errorMessage,
            TimeSpan delay,
            CancellationToken cancellationToken = default)
        {
            var event_ = new AuditEvent
            {
                Id = Guid.NewGuid().ToString(),
                ExecutionId = executionId,
                EventType = AuditEventType.RetryAttempt,
                Timestamp = DateTime.UtcNow,
                Message = $"Retry attempt {attemptNumber} for operation {operation}",
                Context = new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["attemptNumber"] = attemptNumber,
                    ["errorMessage"] = errorMessage,
                    ["delayMs"] = delay.TotalMilliseconds
                }
            };

            _events.Add(event_);

            _logger.LogWarning("Retry attempt {AttemptNumber} for operation {Operation} in execution {ExecutionId}: {ErrorMessage} (Delay: {Delay}ms)", 
                attemptNumber, operation, executionId, errorMessage, delay.TotalMilliseconds);
        }

        public async Task CompleteExecutionAsync(
            string executionId,
            bool success,
            Dictionary<string, object>? finalMetrics = null,
            string? errorMessage = null,
            CancellationToken cancellationToken = default)
        {
            if (!_executions.TryGetValue(executionId, out var execution))
            {
                _logger.LogWarning("Execution {ExecutionId} not found for completion", executionId);
                return;
            }

            execution.EndTime = DateTime.UtcNow;
            execution.Duration = execution.EndTime - execution.StartTime;
            execution.Status = success ? ExecutionStatus.Completed : ExecutionStatus.Failed;
            execution.ErrorMessage = errorMessage;

            if (finalMetrics != null)
            {
                foreach (var metric in finalMetrics)
                {
                    execution.Metrics[metric.Key] = metric.Value;
                }
            }

            var event_ = new AuditEvent
            {
                Id = Guid.NewGuid().ToString(),
                ExecutionId = executionId,
                EventType = success ? AuditEventType.ExecutionCompleted : AuditEventType.ExecutionFailed,
                Timestamp = DateTime.UtcNow,
                Message = success ? $"Execution {executionId} completed" : $"Execution {executionId} failed: {errorMessage}",
                Context = finalMetrics ?? new Dictionary<string, object>()
            };

            _events.Add(event_);

            _logger.LogInformation("Completed execution {ExecutionId}: Success={Success}, Duration={Duration}ms", 
                executionId, success, execution.Duration.TotalMilliseconds);
        }

        public async Task<AuditExecution> GetExecutionAsync(
            string executionId,
            CancellationToken cancellationToken = default)
        {
            if (_executions.TryGetValue(executionId, out var execution))
            {
                return await Task.FromResult(execution);
            }

            return await Task.FromResult(new AuditExecution { Id = executionId });
        }

        public async Task<List<AuditEvent>> GetExecutionEventsAsync(
            string executionId,
            CancellationToken cancellationToken = default)
        {
            var events = _events.Where(e => e.ExecutionId == executionId).ToList();
            return await Task.FromResult(events);
        }

        public async Task<AuditSummary> GetExecutionSummaryAsync(
            string executionId,
            CancellationToken cancellationToken = default)
        {
            if (!_executions.TryGetValue(executionId, out var execution))
            {
                return await Task.FromResult(new AuditSummary { ExecutionId = executionId });
            }

            var events = _events.Where(e => e.ExecutionId == executionId).ToList();
            var stepTimings = execution.Steps.ToDictionary(s => s.Name, s => s.Duration);
            var retryCount = events.Count(e => e.EventType == AuditEventType.RetryAttempt);
            var policyDecisions = events.Count(e => e.EventType == AuditEventType.PolicyDecision);
            var providerDecisions = events.Count(e => e.EventType == AuditEventType.ProviderDecision);

            return await Task.FromResult(new AuditSummary
            {
                ExecutionId = executionId,
                OperationType = execution.OperationType,
                Status = execution.Status,
                StartTime = execution.StartTime,
                EndTime = execution.EndTime,
                Duration = execution.Duration,
                StepCount = execution.Steps.Count,
                StepTimings = stepTimings,
                RetryCount = retryCount,
                PolicyDecisions = policyDecisions,
                ProviderDecisions = providerDecisions,
                ErrorMessage = execution.ErrorMessage,
                Metrics = execution.Metrics
            });
        }
    }

    /// <summary>
    /// Audit execution
    /// </summary>
    public class AuditExecution
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
    public class AuditStep
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
    public class AuditEvent
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
    public class AuditSummary
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
