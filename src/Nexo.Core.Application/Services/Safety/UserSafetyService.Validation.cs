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
    /// Safety validation and risk assessment functionality
    /// </summary>
    public partial class UserSafetyService
    {
        /// <summary>
        /// Validates a user operation for safety risks
        /// </summary>
        public async Task<SafetyCheckResult> ValidateOperationAsync(UserOperation operation)
        {
            _logger.LogDebug("Validating operation: {OperationType} on {TargetPath}", 
                operation.Type, operation.TargetPath);

            var risks = new List<SafetyRisk>();
            var safeguards = new List<SafetySafeguard>();

            // Check for destructive operations
            if (operation.IsDestructive)
            {
                var risk = new SafetyRisk
                {
                    Level = RiskLevel.High,
                    Message = "This operation will modify existing files",
                    Recommendation = "Create backup before proceeding",
                    Category = RiskCategory.DataLoss,
                    AffectedFiles = operation.AffectedFiles
                };
                risks.Add(risk);

                safeguards.Add(new SafetySafeguard
                {
                    Type = SafeguardType.AutomaticBackup,
                    Description = "Automatic backup will be created before operation",
                    IsRequired = true
                });
            }

            // Check for large-scale changes
            if (operation.AffectedFiles > 10)
            {
                var risk = new SafetyRisk
                {
                    Level = RiskLevel.Medium,
                    Message = $"Operation will affect {operation.AffectedFiles} files",
                    Recommendation = "Review changes in dry-run mode first",
                    Category = RiskCategory.Scale,
                    AffectedFiles = operation.AffectedFiles
                };
                risks.Add(risk);

                safeguards.Add(new SafetySafeguard
                {
                    Type = SafeguardType.DryRunMode,
                    Description = "Preview all changes before execution",
                    IsRequired = true
                });
            }

            // Check for system file modifications
            if (IsSystemFile(operation.TargetPath))
            {
                var risk = new SafetyRisk
                {
                    Level = RiskLevel.Critical,
                    Message = "Operation targets system files",
                    Recommendation = "Avoid modifying system files",
                    Category = RiskCategory.SystemIntegrity,
                    AffectedFiles = 1
                };
                risks.Add(risk);

                safeguards.Add(new SafetySafeguard
                {
                    Type = SafeguardType.ConfirmationPrompt,
                    Description = "Additional confirmation required for system files",
                    IsRequired = true
                });
            }

            // Check for concurrent operations
            if (await HasConcurrentOperationsAsync(operation))
            {
                var risk = new SafetyRisk
                {
                    Level = RiskLevel.Medium,
                    Message = "Another operation is in progress",
                    Recommendation = "Wait for current operation to complete",
                    Category = RiskCategory.Concurrency,
                    AffectedFiles = 0
                };
                risks.Add(risk);
            }

            // Check for insufficient permissions
            if (!await HasRequiredPermissionsAsync(operation))
            {
                var risk = new SafetyRisk
                {
                    Level = RiskLevel.High,
                    Message = "Insufficient permissions for this operation",
                    Recommendation = "Run with appropriate permissions or contact administrator",
                    Category = RiskCategory.Permissions,
                    AffectedFiles = 0
                };
                risks.Add(risk);
            }

            var result = new SafetyCheckResult
            {
                OperationId = operation.Id,
                Risks = risks,
                Safeguards = safeguards,
                RequiresConfirmation = risks.Any(r => r.Level >= RiskLevel.Medium),
                RequiresBackup = risks.Any(r => r.Category == RiskCategory.DataLoss),
                IsSafeToProceed = !risks.Any(r => r.Level == RiskLevel.Critical),
                ValidationTimestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Safety validation completed for operation {OperationId}: {RiskCount} risks, {SafeguardCount} safeguards",
                operation.Id, risks.Count, safeguards.Count);

            return result;
        }

        private bool IsSystemFile(string path)
        {
            var systemPaths = new[]
            {
                "/System/",
                "/Windows/",
                "/Program Files/",
                "/usr/bin/",
                "/usr/lib/",
                "/etc/"
            };

            return systemPaths.Any(systemPath => 
                path.StartsWith(systemPath, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<bool> HasConcurrentOperationsAsync(UserOperation operation)
        {
            // Check if there are other operations in progress
            // This would typically query a database or in-memory store
            await Task.Delay(10); // Simulate async operation
            return false; // Simplified for demo
        }

        private async Task<bool> HasRequiredPermissionsAsync(UserOperation operation)
        {
            // Check if user has required permissions for the operation
            // This would typically check file system permissions
            await Task.Delay(10); // Simulate async operation
            return true; // Simplified for demo
        }
    }
}
