using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Step management functionality
    /// </summary>
    public partial class AuditService
    {
        public Task AddStepAsync(
            string executionId,
            string stepName,
            string stepType,
            Dictionary<string, object>? stepContext = null,
            CancellationToken cancellationToken = default)
        {
            if (!_executions.TryGetValue(executionId, out var execution))
            {
                _logger.LogWarning("Execution {ExecutionId} not found for step {StepName}", executionId, stepName);
                return Task.CompletedTask;
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
            return Task.CompletedTask;
        }

        public Task CompleteStepAsync(
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
                return Task.CompletedTask;
            }

            var step = execution.Steps.FirstOrDefault(s => s.Name == stepName);
            if (step == null)
            {
                _logger.LogWarning("Step {StepName} not found in execution {ExecutionId}", stepName, executionId);
                return Task.CompletedTask;
            }

            step.EndTime = DateTime.UtcNow;
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
            return Task.CompletedTask;
        }
    }
}
