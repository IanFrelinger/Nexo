using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Demo.Tests.Support
{
    /// <summary>
    /// Manages approval requests for demo scenarios
    /// </summary>
    public static class ApprovalManager
    {
        private static readonly Dictionary<string, MockApprovalResult> _approvalRequests = new();
        private static readonly List<string> _auditLogs = new();
        private static int _approvalCount = 0;

        /// <summary>
        /// Gets an approval request by ID
        /// </summary>
        public static MockApprovalResult? GetApprovalRequest(string requestId)
        {
            return _approvalRequests.TryGetValue(requestId, out var request) ? request : null;
        }

        /// <summary>
        /// Approves a request
        /// </summary>
        public static void ApproveRequest(string requestId)
        {
            if (_approvalRequests.TryGetValue(requestId, out var request))
            {
                request.Approved = true;
                request.ApprovedAt = DateTime.UtcNow;
                _approvalCount++;
                _auditLogs.Add($"Request {requestId} approved at {request.ApprovedAt:O}");
            }
        }

        /// <summary>
        /// Records an approval with reason
        /// </summary>
        public static void RecordApproval(string approver, string reason)
        {
            _auditLogs.Add($"Approval by {approver}: {reason} at {DateTime.UtcNow:O}");
        }

        /// <summary>
        /// Gets the approval count
        /// </summary>
        public static int GetApprovalCount() => _approvalCount;

        /// <summary>
        /// Gets audit logs
        /// </summary>
        public static List<string> GetAuditLogs() => _auditLogs.ToList();

        /// <summary>
        /// Resets the approval manager state
        /// </summary>
        public static void ResetState()
        {
            _approvalRequests.Clear();
            _auditLogs.Clear();
            _approvalCount = 0;
        }
    }

    /// <summary>
    /// Represents a mock approval result
    /// </summary>
    public class MockApprovalResult
    {
        public string RequestId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Approved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string Approver { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
