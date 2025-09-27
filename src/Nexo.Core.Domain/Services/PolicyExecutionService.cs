using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.Policy;
using Nexo.Core.Domain.Services;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Service for executing policies with pause/resume and approval workflows
    /// </summary>
    public partial class PolicyExecutionService : IPolicyExecutionService
    {
        private readonly ILogger<PolicyExecutionService> _logger;
        private readonly IPolicyEngine _policyEngine;
        private readonly IPolicyApprovalService _approvalService;
        private readonly Dictionary<string, PolicyExecutionState> _executionStates;

        public PolicyExecutionService(
            ILogger<PolicyExecutionService> logger,
            IPolicyEngine policyEngine,
            IPolicyApprovalService approvalService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _policyEngine = policyEngine ?? throw new ArgumentNullException(nameof(policyEngine));
            _approvalService = approvalService ?? throw new ArgumentNullException(nameof(approvalService));
            _executionStates = new Dictionary<string, PolicyExecutionState>();
        }

        public async Task<PolicyExecutionResult> ExecutePolicyAsync(
            string executionId,
            string code,
            PolicyDefinition policy,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing policy for execution {ExecutionId}", executionId);

            var state = new PolicyExecutionState
            {
                ExecutionId = executionId,
                Status = PolicyExecutionStatus.Running,
                StartTime = DateTime.UtcNow
            };
            _executionStates[executionId] = state;

            try
            {
                // Check if policy requires approval
                if (policy.Meta.RequiresApproval)
                {
                    return await HandlePolicyWithApproval(executionId, code, policy, cancellationToken);
                }

                // Execute policy directly
                return await ExecutePolicyDirectly(executionId, code, policy, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing policy for execution {ExecutionId}", executionId);
                state.Status = PolicyExecutionStatus.Failed;
                state.ErrorMessage = ex.Message;
                
                return new PolicyExecutionResult
                {
                    Passed = false,
                    ErrorMessage = ex.Message,
                    ExecutionId = executionId
                };
            }
        }

        public async Task<PolicyExecutionResult> ResumeExecutionAsync(
            string executionId,
            string approvalRequestId,
            CancellationToken cancellationToken = default)
        {
            if (!_executionStates.TryGetValue(executionId, out var state))
            {
                return new PolicyExecutionResult
                {
                    Passed = false,
                    ErrorMessage = "Execution state not found"
                };
            }

            if (state.Status != PolicyExecutionStatus.Paused)
            {
                return new PolicyExecutionResult
                {
                    Passed = false,
                    ErrorMessage = "Execution is not paused"
                };
            }

            // Check if approval was granted
            var hasApproval = await _approvalService.HasApprovalAsync(executionId, state.PolicyId!, cancellationToken);
            if (!hasApproval)
            {
                return new PolicyExecutionResult
                {
                    Passed = false,
                    ErrorMessage = "No approval found for this execution"
                };
            }

            // Resume execution
            state.Status = PolicyExecutionStatus.Running;
            state.ResumedAt = DateTime.UtcNow;

            _logger.LogInformation("Resuming policy execution {ExecutionId} after approval", executionId);

            // Continue with policy execution
            return await ExecutePolicyDirectly(executionId, state.Code!, state.Policy!, cancellationToken);
        }

        public async Task<bool> PauseExecutionAsync(
            string executionId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (!_executionStates.TryGetValue(executionId, out var state))
            {
                return false;
            }

            state.Status = PolicyExecutionStatus.Paused;
            state.PausedAt = DateTime.UtcNow;
            state.PauseReason = reason;

            _logger.LogInformation("Paused policy execution {ExecutionId}: {Reason}", executionId, reason);
            return await Task.FromResult(true);
        }

        public async Task<PolicyExecutionState> GetExecutionStateAsync(
            string executionId,
            CancellationToken cancellationToken = default)
        {
            if (_executionStates.TryGetValue(executionId, out var state))
            {
                return await Task.FromResult(state);
            }
            return await Task.FromResult(new PolicyExecutionState { ExecutionId = executionId });
        }

        private async Task<PolicyExecutionResult> HandlePolicyWithApproval(
            string executionId,
            string code,
            PolicyDefinition policy,
            CancellationToken cancellationToken)
        {
            var state = _executionStates[executionId];
            state.Policy = policy;
            state.Code = code;
            state.PolicyId = policy.Meta.Id;

            // Create approval request
            var approvalRequest = await _approvalService.CreateApprovalRequestAsync(
                executionId,
                policy.Meta.Id,
                $"Policy {policy.Meta.Id} requires approval",
                new Dictionary<string, object> { ["code"] = code },
                cancellationToken);

            // Pause execution
            await PauseExecutionAsync(executionId, "Waiting for policy approval", cancellationToken);

            return new PolicyExecutionResult
            {
                Passed = false,
                RequiresApproval = true,
                ApprovalRequestId = approvalRequest.Id,
                ExecutionId = executionId,
                Message = "Policy execution paused pending approval"
            };
        }

        private async Task<PolicyExecutionResult> ExecutePolicyDirectly(
            string executionId,
            string code,
            PolicyDefinition policy,
            CancellationToken cancellationToken)
        {
            var state = _executionStates[executionId];

            // Execute safety policy
            SafetyPolicyResult? safetyResult = null;
            if (policy.Safety != null)
            {
                safetyResult = await _policyEngine.ApplySafetyPolicyAsync(code, policy.Safety, cancellationToken);
            }

            // Execute quality policy
            QualityPolicyResult? qualityResult = null;
            if (policy.Quality != null)
            {
                qualityResult = await _policyEngine.ApplyQualityPolicyAsync(code, policy.Quality, cancellationToken);
            }

            // Determine overall result
            var passed = (safetyResult?.Passed ?? true) && (qualityResult?.Passed ?? true);
            
            state.Status = passed ? PolicyExecutionStatus.Completed : PolicyExecutionStatus.Failed;
            state.CompletedAt = DateTime.UtcNow;

            return new PolicyExecutionResult
            {
                Passed = passed,
                SafetyResult = safetyResult,
                QualityResult = qualityResult,
                ExecutionId = executionId,
                ExecutionTimeMs = state.CompletedAt.HasValue ? (long)(state.CompletedAt.Value - state.StartTime).TotalMilliseconds : 0
            };
        }
    }

    /// <summary>
    /// Policy execution state
    /// </summary>
    public partial class PolicyExecutionState
    {
        public string ExecutionId { get; set; } = string.Empty;
        public PolicyExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? PausedAt { get; set; }
        public DateTime? ResumedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? PauseReason { get; set; }
        public string? ErrorMessage { get; set; }
        public PolicyDefinition? Policy { get; set; }
        public string? Code { get; set; }
        public string? PolicyId { get; set; }
    }

    /// <summary>
    /// Policy execution status
    /// </summary>
    public enum PolicyExecutionStatus
    {
        Running,
        Paused,
        Completed,
        Failed
    }

    /// <summary>
    /// Interface for policy execution service
    /// </summary>
    public interface IPolicyExecutionService
    {
        Task<PolicyExecutionResult> ExecutePolicyAsync(
            string executionId,
            string code,
            PolicyDefinition policy,
            CancellationToken cancellationToken = default);

        Task<PolicyExecutionResult> ResumeExecutionAsync(
            string executionId,
            string approvalRequestId,
            CancellationToken cancellationToken = default);

        Task<bool> PauseExecutionAsync(
            string executionId,
            string reason,
            CancellationToken cancellationToken = default);

        Task<PolicyExecutionState> GetExecutionStateAsync(
            string executionId,
            CancellationToken cancellationToken = default);
    }
}
