using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Decision tracking functionality
    /// </summary>
    public partial class AuditService
    {
        public Task AddProviderDecisionAsync(
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
            return Task.CompletedTask;
        }

        public Task AddPolicyDecisionAsync(
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
            return Task.CompletedTask;
        }

        public Task AddRetryAttemptAsync(
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
            return Task.CompletedTask;
        }
    }
}
