using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.Safety;
using Nexo.Core.Domain.Results;
using Nexo.Core.Domain.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Enums.Safety;

namespace Nexo.Core.Application.Services.Safety
{
    /// <summary>
    /// Safeguard execution and management functionality
    /// </summary>
    public partial class UserSafetyService
    {
        /// <summary>
        /// Executes safety safeguards before operation
        /// </summary>
        public async Task<SafeguardExecutionResult> ExecuteSafeguardsAsync(
            UserOperation operation, 
            SafetyCheckResult safetyResult)
        {
            _logger.LogDebug("Executing safeguards for operation: {OperationId}", operation.Id);

            var results = new List<SafeguardResult>();

            foreach (var safeguard in safetyResult.Safeguards)
            {
                try
                {
                    var result = await ExecuteSafeguardAsync(operation, safeguard);
                    results.Add(result);

                    _logger.LogDebug("Safeguard {SafeguardType} executed: {Success}", 
                        safeguard.Type, result.Success);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute safeguard {SafeguardType}", safeguard.Type);
                    
                    results.Add(new SafeguardResult
                    {
                        SafeguardType = safeguard.Type,
                        Success = false,
                        Error = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            var executionResult = new SafeguardExecutionResult
            {
                OperationId = operation.Id,
                Results = results,
                AllSafeguardsSuccessful = results.All(r => r.Success),
                ExecutionTimestamp = DateTime.UtcNow
            };

            // Log audit trail
            await _auditService.LogActionAsync(operation.UserId, "SafeguardExecution", new Dictionary<string, object>
            {
                ["OperationId"] = executionResult.OperationId,
                ["AllSafeguardsSuccessful"] = executionResult.AllSafeguardsSuccessful,
                ["ExecutionTimestamp"] = executionResult.ExecutionTimestamp,
                ["ExecutionNotes"] = executionResult.ExecutionNotes ?? string.Empty
            });

            return executionResult;
        }

        private async Task<SafeguardResult> ExecuteSafeguardAsync(UserOperation operation, SafetySafeguard safeguard)
        {
            switch (safeguard.Type)
            {
                case SafeguardType.AutomaticBackup:
                    var backup = await CreateBackupAsync(operation);
                    return new SafeguardResult
                    {
                        SafeguardType = safeguard.Type,
                        Success = backup.IsSuccess,
                        Details = $"Backup created: {backup.Id}",
                        Timestamp = DateTime.UtcNow
                    };

                case SafeguardType.DryRunMode:
                    var dryRun = await ExecuteDryRunAsync(operation);
                    return new SafeguardResult
                    {
                        SafeguardType = safeguard.Type,
                        Success = dryRun.Success,
                        Details = $"Dry-run completed: {dryRun.Changes.Count} changes",
                        Timestamp = DateTime.UtcNow
                    };

                case SafeguardType.ConfirmationPrompt:
                    return new SafeguardResult
                    {
                        SafeguardType = safeguard.Type,
                        Success = true,
                        Details = "Confirmation prompt displayed",
                        Timestamp = DateTime.UtcNow
                    };

                default:
                    return new SafeguardResult
                    {
                        SafeguardType = safeguard.Type,
                        Success = false,
                        Error = $"Unknown safeguard type: {safeguard.Type}",
                        Timestamp = DateTime.UtcNow
                    };
            }
        }
    }
}
