using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Reporting and querying functionality
    /// </summary>
    public partial class AuditService
    {
        public Task<List<AuditEvent>> GetExecutionEventsAsync(
            string executionId,
            CancellationToken cancellationToken = default)
        {
            var events = _events.Where(e => e.ExecutionId == executionId).ToList();
            return Task.FromResult(events);
        }

        public Task<AuditSummary> GetExecutionSummaryAsync(
            string executionId,
            CancellationToken cancellationToken = default)
        {
            if (!_executions.TryGetValue(executionId, out var execution))
            {
                return Task.FromResult(new AuditSummary { ExecutionId = executionId });
            }

            var events = _events.Where(e => e.ExecutionId == executionId).ToList();
            var stepTimings = execution.Steps.ToDictionary(s => s.Name, s => s.Duration);
            var retryCount = events.Count(e => e.EventType == AuditEventType.RetryAttempt);
            var policyDecisions = events.Count(e => e.EventType == AuditEventType.PolicyDecision);
            var providerDecisions = events.Count(e => e.EventType == AuditEventType.ProviderDecision);

            return Task.FromResult(new AuditSummary
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
}
