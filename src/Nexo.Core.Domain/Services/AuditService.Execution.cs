using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Execution management functionality
    /// </summary>
    public partial class AuditService
    {
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

        public Task CompleteExecutionAsync(
            string executionId,
            bool success,
            Dictionary<string, object>? finalMetrics = null,
            string? errorMessage = null,
            CancellationToken cancellationToken = default)
        {
            if (!_executions.TryGetValue(executionId, out var execution))
            {
                _logger.LogWarning("Execution {ExecutionId} not found for completion", executionId);
                return Task.CompletedTask;
            }

            execution.EndTime = DateTime.UtcNow;
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
            return Task.CompletedTask;
        }

        public Task<AuditExecution> GetExecutionAsync(
            string executionId,
            CancellationToken cancellationToken = default)
        {
            if (_executions.TryGetValue(executionId, out var execution))
            {
                return Task.FromResult(execution);
            }

            return Task.FromResult(new AuditExecution { Id = executionId });
        }
    }
}
