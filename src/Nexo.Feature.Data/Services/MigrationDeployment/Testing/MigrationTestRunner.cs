using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;

namespace Nexo.Feature.Data.Services.MigrationDeployment.Testing
{
    /// <summary>
    /// Runs migration-related tests
    /// </summary>
    public partial class MigrationTestRunner
    {
        private readonly IDatabaseProvider _databaseProvider;
        private readonly IMigrationService _migrationService;
        private readonly ILogger _logger;

        public MigrationTestRunner(IDatabaseProvider databaseProvider, IMigrationService migrationService, ILogger logger)
        {
            _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
            _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Run pre-deployment tests
        /// </summary>
        public async Task<MigrationTestResult> RunPreDeploymentTestsAsync(CancellationToken cancellationToken = default)
        {
            var result = new MigrationTestResult();
            
            try
            {
                _logger.LogInformation("Running pre-deployment tests");

                // Test database connectivity
                var healthResult = await _databaseProvider.GetHealthStatusAsync(cancellationToken);
                if (!healthResult.IsHealthy)
                {
                    result.Errors.Add($"Database health check failed: {healthResult.ErrorMessage}");
                }

                // Test schema version retrieval
                var schemaVersion = await _migrationService.GetSchemaVersionAsync(cancellationToken);
                if (string.IsNullOrEmpty(schemaVersion))
                {
                    result.Warnings.Add("Unable to retrieve current schema version");
                }

                // Test migration listing
                var migrations = await _migrationService.GetMigrationsAsync(cancellationToken);
                if (!migrations.Any())
                {
                    result.Warnings.Add("No migrations found");
                }

                result.IsSuccessful = !result.Errors.Any();
                _logger.LogInformation("Pre-deployment tests completed: {Success}", result.IsSuccessful);
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Errors.Add($"Pre-deployment testing failed: {ex.Message}");
                _logger.LogError(ex, "Pre-deployment testing failed");
            }

            return result;
        }

        /// <summary>
        /// Run post-deployment tests
        /// </summary>
        public async Task<MigrationTestResult> RunPostDeploymentTestsAsync(CancellationToken cancellationToken = default)
        {
            var result = new MigrationTestResult();
            
            try
            {
                _logger.LogInformation("Running post-deployment tests");

                // Test database connectivity
                var healthResult = await _databaseProvider.GetHealthStatusAsync(cancellationToken);
                if (!healthResult.IsHealthy)
                {
                    result.Errors.Add($"Database health check failed: {healthResult.ErrorMessage}");
                }

                // Test schema version
                var schemaVersion = await _migrationService.GetSchemaVersionAsync(cancellationToken);
                if (string.IsNullOrEmpty(schemaVersion))
                {
                    result.Errors.Add("Unable to retrieve schema version after deployment");
                }

                // Test basic queries (if any tables exist)
                try
                {
                    var testQuery = "SELECT 1";
                    await _databaseProvider.QueryAsync<object>(testQuery, null, cancellationToken);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Basic query test failed: {ex.Message}");
                }

                result.IsSuccessful = !result.Errors.Any();
                _logger.LogInformation("Post-deployment tests completed: {Success}", result.IsSuccessful);
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Errors.Add($"Post-deployment testing failed: {ex.Message}");
                _logger.LogError(ex, "Post-deployment testing failed");
            }

            return result;
        }
    }
}
