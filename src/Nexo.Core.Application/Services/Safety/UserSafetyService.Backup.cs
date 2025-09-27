using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.Safety;
using Nexo.Core.Domain.Results;
using Nexo.Core.Domain.Requests;
using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Enums.Safety;

namespace Nexo.Core.Application.Services.Safety
{
    /// <summary>
    /// Backup creation and management functionality
    /// </summary>
    public partial class UserSafetyService
    {
        /// <summary>
        /// Creates automatic backup before destructive operations
        /// </summary>
        public async Task<BackupResult> CreateBackupAsync(UserOperation operation)
        {
            _logger.LogInformation("Creating backup for operation: {OperationType}", operation.Type);

            try
            {
                var backup = await _backupService.CreateBackupAsync(operation.UserId, BackupType.Full);

                _logger.LogInformation("Backup created successfully: {BackupId}", backup);
                return new BackupResult { Id = backup };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create backup for operation {OperationId}", operation.Id);
                throw;
            }
        }
    }
}
