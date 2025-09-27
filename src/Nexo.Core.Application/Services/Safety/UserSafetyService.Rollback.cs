using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.Safety;
using Nexo.Core.Domain.Results;
using System;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Safety
{
    /// <summary>
    /// Rollback operations and recovery functionality
    /// </summary>
    public partial class UserSafetyService
    {
        /// <summary>
        /// Provides rollback capability for operations
        /// </summary>
        public async Task<RollbackResult> RollbackOperationAsync(string operationId)
        {
            _logger.LogInformation("Rolling back operation: {OperationId}", operationId);

            try
            {
                // Find the backup for this operation
                var backup = await _backupService.GetBackupByOperationIdAsync(operationId);
                if (backup == null)
                {
                    throw new InvalidOperationException($"No backup found for operation {operationId}");
                }

                // Restore from backup
                var restoreResult = await _backupService.RestoreFromBackupAsync(operationId, backup.Id);

                var result = new RollbackResult
                {
                    OperationId = operationId,
                    BackupId = backup.Id,
                    Success = restoreResult,
                    RestoredFiles = 0, // TODO: Get actual restored files count
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Rollback completed for operation {OperationId}: {Success}",
                    operationId, result.Success);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rollback failed for operation {OperationId}", operationId);
                
                return new RollbackResult
                {
                    OperationId = operationId,
                    Success = false,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }
}
