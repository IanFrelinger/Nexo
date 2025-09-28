using Nexo.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Represents the result of an agent operation
    /// </summary>
    public sealed class AgentResult
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        public string? WarningMessage { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }
        public TimeSpan ExecutionDuration { get; }
        public DateTimeOffset StartedAt { get; }
        public DateTimeOffset CompletedAt { get; }
        public AgentId? AgentId { get; }
        public string? OperationType { get; }
        public IReadOnlyList<AgentResult> SubResults { get; }

        public AgentResult(
            bool isSuccess,
            string? errorMessage = null,
            string? warningMessage = null,
            IEnumerable<string>? warnings = null,
            IDictionary<string, object>? metadata = null,
            TimeSpan? executionDuration = null,
            DateTimeOffset? startedAt = null,
            DateTimeOffset? completedAt = null,
            AgentId? agentId = null,
            string? operationType = null,
            IEnumerable<AgentResult>? subResults = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            WarningMessage = warningMessage;
            Warnings = (warnings?.ToList() ?? new List<string>()).AsReadOnly();
            Metadata = (metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()).AsReadOnly();
            ExecutionDuration = executionDuration ?? TimeSpan.Zero;
            StartedAt = startedAt ?? DateTimeOffset.UtcNow;
            CompletedAt = completedAt ?? DateTimeOffset.UtcNow;
            AgentId = agentId;
            OperationType = operationType;
            SubResults = (subResults?.ToList() ?? new List<AgentResult>()).AsReadOnly();
        }

        /// <summary>
        /// Create a successful result
        /// </summary>
        public static AgentResult Success(
            string? message = null,
            IDictionary<string, object>? metadata = null,
            TimeSpan? executionDuration = null,
            AgentId? agentId = null,
            string? operationType = null,
            IEnumerable<AgentResult>? subResults = null)
        {
            return new AgentResult(
                true,
                metadata: metadata,
                executionDuration: executionDuration,
                agentId: agentId,
                operationType: operationType,
                subResults: subResults);
        }

        /// <summary>
        /// Create a failed result
        /// </summary>
        public static AgentResult Failure(
            string errorMessage,
            IDictionary<string, object>? metadata = null,
            TimeSpan? executionDuration = null,
            AgentId? agentId = null,
            string? operationType = null,
            IEnumerable<AgentResult>? subResults = null)
        {
            return new AgentResult(
                false,
                errorMessage: errorMessage,
                metadata: metadata,
                executionDuration: executionDuration,
                agentId: agentId,
                operationType: operationType,
                subResults: subResults);
        }

        /// <summary>
        /// Create a result with warnings
        /// </summary>
        public static AgentResult SuccessWithWarnings(
            string warningMessage,
            IEnumerable<string>? warnings = null,
            IDictionary<string, object>? metadata = null,
            TimeSpan? executionDuration = null,
            AgentId? agentId = null,
            string? operationType = null,
            IEnumerable<AgentResult>? subResults = null)
        {
            return new AgentResult(
                true,
                warningMessage: warningMessage,
                warnings: warnings,
                metadata: metadata,
                executionDuration: executionDuration,
                agentId: agentId,
                operationType: operationType,
                subResults: subResults);
        }

        /// <summary>
        /// Create a result from an exception
        /// </summary>
        public static AgentResult FromException(
            Exception exception,
            AgentId? agentId = null,
            string? operationType = null,
            TimeSpan? executionDuration = null)
        {
            return new AgentResult(
                false,
                errorMessage: exception.Message,
                metadata: new Dictionary<string, object>
                {
                    ["exceptionType"] = exception.GetType().Name,
                    ["stackTrace"] = exception.StackTrace ?? string.Empty
                },
                executionDuration: executionDuration,
                agentId: agentId,
                operationType: operationType);
        }

        /// <summary>
        /// Create a result from multiple sub-results
        /// </summary>
        public static AgentResult FromSubResults(
            IEnumerable<AgentResult> subResults,
            AgentId? agentId = null,
            string? operationType = null,
            TimeSpan? executionDuration = null)
        {
            var results = subResults.ToList();
            var isSuccess = results.All(r => r.IsSuccess);
            var errorMessages = results.Where(r => !r.IsSuccess).Select(r => r.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            var warningMessages = results.Where(r => !string.IsNullOrEmpty(r.WarningMessage)).Select(r => r.WarningMessage);
            var allWarnings = results.SelectMany(r => r.Warnings).ToList();

            var metadata = new Dictionary<string, object>
            {
                ["subResultCount"] = results.Count,
                ["successCount"] = results.Count(r => r.IsSuccess),
                ["failureCount"] = results.Count(r => !r.IsSuccess)
            };

            if (errorMessages.Any())
            {
                metadata["errorMessages"] = errorMessages.ToList();
            }

            if (warningMessages.Any())
            {
                metadata["warningMessages"] = warningMessages.ToList();
            }

            return new AgentResult(
                isSuccess,
                errorMessage: isSuccess ? null : string.Join("; ", errorMessages),
                warningMessage: warningMessages.FirstOrDefault(),
                warnings: allWarnings,
                metadata: metadata,
                executionDuration: executionDuration,
                agentId: agentId,
                operationType: operationType,
                subResults: results);
        }

        /// <summary>
        /// Get a metadata value
        /// </summary>
        public T? GetMetadata<T>(string key)
        {
            if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Check if result has warnings
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0 || !string.IsNullOrEmpty(WarningMessage);

        /// <summary>
        /// Check if result has sub-results
        /// </summary>
        public bool HasSubResults => SubResults.Count > 0;

        /// <summary>
        /// Get all error messages including sub-results
        /// </summary>
        public IEnumerable<string> GetAllErrorMessages()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                yield return ErrorMessage;

            foreach (var subResult in SubResults)
            {
                foreach (var error in subResult.GetAllErrorMessages())
                {
                    yield return error;
                }
            }
        }

        /// <summary>
        /// Get all warning messages including sub-results
        /// </summary>
        public IEnumerable<string> GetAllWarningMessages()
        {
            if (!string.IsNullOrEmpty(WarningMessage))
                yield return WarningMessage;

            foreach (var warning in Warnings)
            {
                yield return warning;
            }

            foreach (var subResult in SubResults)
            {
                foreach (var warning in subResult.GetAllWarningMessages())
                {
                    yield return warning;
                }
            }
        }

        /// <summary>
        /// Get the total execution duration including sub-results
        /// </summary>
        public TimeSpan GetTotalExecutionDuration()
        {
            var totalDuration = ExecutionDuration;
            foreach (var subResult in SubResults)
            {
                totalDuration += subResult.GetTotalExecutionDuration();
            }
            return totalDuration;
        }

        /// <summary>
        /// Get a summary of the result
        /// </summary>
        public string GetSummary()
        {
            var status = IsSuccess ? "Success" : "Failed";
            var duration = ExecutionDuration.TotalMilliseconds.ToString("F2");
            var message = IsSuccess ? WarningMessage : ErrorMessage;
            
            var summary = $"{status} ({duration}ms)";
            if (!string.IsNullOrEmpty(message))
            {
                summary += $": {message}";
            }
            
            if (HasSubResults)
            {
                var successCount = SubResults.Count(r => r.IsSuccess);
                var totalCount = SubResults.Count;
                summary += $" [{successCount}/{totalCount} sub-results]";
            }
            
            return summary;
        }
    }
}
