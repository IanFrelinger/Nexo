using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Contracts;

namespace Nexo.Core.Pipeline
{
    /// <summary>
    /// Canary deployment functionality for ExtensionPipeline.
    /// </summary>
    public sealed partial class ExtensionPipeline<TRequest, TArtifact>
    {
        /// <summary>
        /// Executes canary deployment if enabled
        /// </summary>
        private async Task<bool> ExecuteCanaryDeployment(GenerationResult<TArtifact> currentGenerationResult, List<string> notes, CancellationToken ct)
        {
            if (!_repairOptions.EnableCanaryDeployment || _canaryDeployer == null)
            {
                return true; // Canary deployment not enabled, consider it successful
            }

            _logger.LogInformation("Step 4: Canary deployment");
            var canaryStopwatch = Stopwatch.StartNew();

            try
            {
                var canaryResult = await _canaryDeployer.CanaryAsync(currentGenerationResult.Artifact, ct);
                canaryStopwatch.Stop();

                if (canaryResult.Healthy)
                {
                    _logger.LogInformation("Canary deployment successful: {Note}", canaryResult.Note);
                    notes.Add($"Canary deployment successful (ID: {canaryResult.CanaryId}): {canaryResult.Note}");
                    notes.Add($"Canary deployment completed in {canaryStopwatch.Elapsed.TotalMilliseconds:F2}ms");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Canary deployment failed: {Note}", canaryResult.Note);
                    notes.Add($"Canary deployment failed: {canaryResult.Note}");

                    // Attempt rollback if enabled and available
                    if (_repairOptions.EnableAutomaticRollback && _rollbackStrategy != null)
                    {
                        await AttemptRollback(currentGenerationResult, "Canary deployment failed", notes, ct);
                    }

                    return false;
                }
            }
            catch (Exception canaryEx)
            {
                _logger.LogError(canaryEx, "Canary deployment threw exception");
                notes.Add($"Canary deployment failed with exception: {canaryEx.Message}");

                // Attempt rollback on canary exception
                if (_repairOptions.EnableAutomaticRollback && _rollbackStrategy != null)
                {
                    await AttemptRollback(currentGenerationResult, "Canary deployment exception", notes, ct);
                }

                return false;
            }
        }

        /// <summary>
        /// Attempts rollback on failure
        /// </summary>
        private async Task AttemptRollback(GenerationResult<TArtifact> currentGenerationResult, string reason, List<string> notes, CancellationToken ct)
        {
            if (_rollbackStrategy == null) return;

            _logger.LogInformation("Attempting rollback due to: {Reason}", reason);
            try
            {
                var rollbackResult = await _rollbackStrategy.RollbackAsync(currentGenerationResult.Artifact, reason, ct);
                
                _logger.LogInformation("Rollback completed: {Result}", rollbackResult);
                notes.Add($"Rollback completed: {rollbackResult}");
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Rollback failed");
                notes.Add($"Rollback failed with exception: {rollbackEx.Message}");
            }
        }
    }
}
