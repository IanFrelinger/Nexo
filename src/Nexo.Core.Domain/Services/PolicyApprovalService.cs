using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Policy;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Service for handling policy approvals and pause/resume workflows
    /// </summary>
    public partial class PolicyApprovalService : IPolicyApprovalService
    {
        private readonly ILogger<PolicyApprovalService> _logger;
        private readonly Dictionary<string, PolicyApprovalRequest> _pendingApprovals;
        private readonly Dictionary<string, List<PolicyApproval>> _approvalHistory;

        public PolicyApprovalService(ILogger<PolicyApprovalService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pendingApprovals = new Dictionary<string, PolicyApprovalRequest>();
            _approvalHistory = new Dictionary<string, List<PolicyApproval>>();
        }

        public async Task<PolicyApprovalRequest> CreateApprovalRequestAsync(
            string executionId,
            string policyId,
            string reason,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default)
        {
            var request = new PolicyApprovalRequest
            {
                Id = Guid.NewGuid().ToString(),
                ExecutionId = executionId,
                PolicyId = policyId,
                Reason = reason,
                Context = context,
                Status = PolicyApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                RequestedBy = "system" // In real implementation, get from current user context
            };

            _pendingApprovals[request.Id] = request;
            
            _logger.LogInformation("Created policy approval request {RequestId} for execution {ExecutionId}", 
                request.Id, executionId);

            return await Task.FromResult(request);
        }

        public async Task<PolicyApprovalResult> ApproveRequestAsync(
            string requestId,
            string approverId,
            string approvalReason,
            CancellationToken cancellationToken = default)
        {
            if (!_pendingApprovals.TryGetValue(requestId, out var request))
            {
                return new PolicyApprovalResult
                {
                    Success = false,
                    ErrorMessage = "Approval request not found"
                };
            }

            var approval = new PolicyApproval
            {
                Id = Guid.NewGuid().ToString(),
                RequestId = requestId,
                ApproverId = approverId,
                Reason = approvalReason,
                ApprovedAt = DateTime.UtcNow,
                Status = PolicyApprovalStatus.Approved
            };

            // Add to history
            if (!_approvalHistory.ContainsKey(request.ExecutionId))
            {
                _approvalHistory[request.ExecutionId] = new List<PolicyApproval>();
            }
            _approvalHistory[request.ExecutionId].Add(approval);

            // Update request status
            request.Status = PolicyApprovalStatus.Approved;
            request.ApprovedAt = DateTime.UtcNow;
            request.ApprovedBy = approverId;

            _logger.LogInformation("Approved policy request {RequestId} by {ApproverId}", 
                requestId, approverId);

            return await Task.FromResult(new PolicyApprovalResult
            {
                Success = true,
                Approval = approval
            });
        }

        public async Task<PolicyApprovalResult> RejectRequestAsync(
            string requestId,
            string approverId,
            string rejectionReason,
            CancellationToken cancellationToken = default)
        {
            if (!_pendingApprovals.TryGetValue(requestId, out var request))
            {
                return new PolicyApprovalResult
                {
                    Success = false,
                    ErrorMessage = "Approval request not found"
                };
            }

            var approval = new PolicyApproval
            {
                Id = Guid.NewGuid().ToString(),
                RequestId = requestId,
                ApproverId = approverId,
                Reason = rejectionReason,
                ApprovedAt = DateTime.UtcNow,
                Status = PolicyApprovalStatus.Rejected
            };

            // Add to history
            if (!_approvalHistory.ContainsKey(request.ExecutionId))
            {
                _approvalHistory[request.ExecutionId] = new List<PolicyApproval>();
            }
            _approvalHistory[request.ExecutionId].Add(approval);

            // Update request status
            request.Status = PolicyApprovalStatus.Rejected;
            request.ApprovedAt = DateTime.UtcNow;
            request.ApprovedBy = approverId;

            _logger.LogInformation("Rejected policy request {RequestId} by {ApproverId}: {Reason}", 
                requestId, approverId, rejectionReason);

            return await Task.FromResult(new PolicyApprovalResult
            {
                Success = true,
                Approval = approval
            });
        }

        public async Task<List<PolicyApprovalRequest>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default)
        {
            var pending = new List<PolicyApprovalRequest>();
            foreach (var request in _pendingApprovals.Values)
            {
                if (request.Status == PolicyApprovalStatus.Pending)
                {
                    pending.Add(request);
                }
            }
            return await Task.FromResult(pending);
        }

        public async Task<List<PolicyApproval>> GetApprovalHistoryAsync(
            string executionId,
            CancellationToken cancellationToken = default)
        {
            if (_approvalHistory.TryGetValue(executionId, out var history))
            {
                return await Task.FromResult(history);
            }
            return await Task.FromResult(new List<PolicyApproval>());
        }

        public async Task<bool> HasApprovalAsync(
            string executionId,
            string policyId,
            CancellationToken cancellationToken = default)
        {
            if (_approvalHistory.TryGetValue(executionId, out var history))
            {
                return await Task.FromResult(history.Exists(a => 
                    a.Status == PolicyApprovalStatus.Approved && 
                    _pendingApprovals.TryGetValue(a.RequestId, out var req) && 
                    req.PolicyId == policyId));
            }
            return await Task.FromResult(false);
        }
    }

    /// <summary>
    /// Policy approval request
    /// </summary>
    public partial class PolicyApprovalRequest
    {
        public string Id { get; set; } = string.Empty;
        public string ExecutionId { get; set; } = string.Empty;
        public string PolicyId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public Dictionary<string, object> Context { get; set; } = new();
        public PolicyApprovalStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
    }

    /// <summary>
    /// Policy approval
    /// </summary>
    public partial class PolicyApproval
    {
        public string Id { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string ApproverId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime ApprovedAt { get; set; }
        public PolicyApprovalStatus Status { get; set; }
    }

    /// <summary>
    /// Policy approval status
    /// </summary>
    public enum PolicyApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }

    /// <summary>
    /// Policy approval result
    /// </summary>
    public partial class PolicyApprovalResult
    {
        public bool Success { get; set; }
        public PolicyApproval? Approval { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface for policy approval service
    /// </summary>
    public interface IPolicyApprovalService
    {
        Task<PolicyApprovalRequest> CreateApprovalRequestAsync(
            string executionId,
            string policyId,
            string reason,
            Dictionary<string, object> context,
            CancellationToken cancellationToken = default);

        Task<PolicyApprovalResult> ApproveRequestAsync(
            string requestId,
            string approverId,
            string approvalReason,
            CancellationToken cancellationToken = default);

        Task<PolicyApprovalResult> RejectRequestAsync(
            string requestId,
            string approverId,
            string rejectionReason,
            CancellationToken cancellationToken = default);

        Task<List<PolicyApprovalRequest>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);

        Task<List<PolicyApproval>> GetApprovalHistoryAsync(
            string executionId,
            CancellationToken cancellationToken = default);

        Task<bool> HasApprovalAsync(
            string executionId,
            string policyId,
            CancellationToken cancellationToken = default);
    }
}
