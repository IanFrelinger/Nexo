using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;

namespace Nexo.Feature.Data.Services.MigrationDeployment.Execution
{
    /// <summary>
    /// Orchestrates migration execution operations
    /// </summary>
    public partial class MigrationExecutionOrchestrator
    {
        private readonly IMigrationService _migrationService;
        private readonly ILogger _logger;

        public MigrationExecutionOrchestrator(IMigrationService migrationService, ILogger logger)
        {
            _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Execute all pending migrations
        /// </summary>
        public async Task<MigrationResult> ExecuteMigrationsAsync(CancellationToken cancellationToken = default)
        {
            return await _migrationService.ApplyMigrationsAsync(cancellationToken);
        }

        /// <summary>
        /// Execute a single migration
        /// </summary>
        public async Task<MigrationResult> ExecuteSingleMigrationAsync(string migrationId, CancellationToken cancellationToken = default)
        {
            return await _migrationService.ApplyMigrationAsync(migrationId, cancellationToken);
        }

        /// <summary>
        /// Rollback to last stable state
        /// </summary>
        public async Task<MigrationResult> RollbackToLastStableStateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogWarning("Rolling back to last stable state");
                return await _migrationService.RollbackLastMigrationAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback to last stable state");
                return new MigrationResult
                {
                    IsSuccessful = false,
                    Message = $"Rollback failed: {ex.Message}",
                    ExecutedAt = DateTime.UtcNow
                };
            }
        }
    }
}
