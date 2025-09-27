using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Contracts;
using Nexo.Observability.ActivitySources;
using Nexo.Observability.Metrics;

namespace Nexo.Core.Pipeline
{
    /// <summary>
    /// Core pipeline execution functionality for ExtensionPipeline.
    /// </summary>
    public sealed partial class ExtensionPipeline<TRequest, TArtifact>
    {
        /// <summary>
        /// Runs the complete pipeline for the given request with recovery loop support.
        /// </summary>
        /// <param name="request">The request to process</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The pipeline report</returns>
        public async Task<PipelineReport> RunAsync(TRequest request, CancellationToken ct = default)
        {
            var pipelineStopwatch = Stopwatch.StartNew();
            var notes = new List<string>();
            var validationResults = new List<ValidationResult>();
            var currentGenerationResult = (GenerationResult<TArtifact>?)null;

            using var pipelineActivity = _pipelineActivitySource.StartActivity("pipeline.run");
            pipelineActivity?.SetTag("request.type", typeof(TRequest).Name);
            pipelineActivity?.SetTag("pipeline.id", Guid.NewGuid().ToString("N")[..8]);

            try
            {
                _logger.LogInformation("Starting pipeline execution for request type {RequestType}", typeof(TRequest).Name);

                // Step 1: Generate the artifact
                currentGenerationResult = await ExecuteGenerationStep(request, notes, ct);

                // Step 2: Run validation and repair loop
                var repairSuccess = await ExecuteValidationAndRepairLoop(currentGenerationResult, validationResults, notes, ct);

                if (!repairSuccess)
                {
                    return CreateFailureReport(notes, "Pipeline failed after repair attempts");
                }

                // Step 3: Execute canary deployment if enabled
                var canarySuccess = await ExecuteCanaryDeployment(currentGenerationResult, notes, ct);

                if (!canarySuccess)
                {
                    return CreateFailureReport(notes, "Canary deployment failed");
                }

                // Step 4: Publish the result
                var finalReport = await ExecutePublishingStep(currentGenerationResult, validationResults, notes, ct);

                pipelineStopwatch.Stop();
                _metrics.RecordPipelineSuccess("extension_pipeline", pipelineStopwatch.Elapsed.TotalMilliseconds);
                pipelineActivity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Pipeline completed successfully in {TotalDuration}ms", pipelineStopwatch.Elapsed.TotalMilliseconds);

                return finalReport;
            }
            catch (Exception ex)
            {
                pipelineStopwatch.Stop();
                _metrics.RecordPipelineFailure("extension_pipeline", pipelineStopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name);
                pipelineActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Pipeline execution failed after {Duration}ms", pipelineStopwatch.Elapsed.TotalMilliseconds);

                notes.Add($"Pipeline failed with exception: {ex.Message}");
                notes.Add($"Pipeline duration: {pipelineStopwatch.Elapsed.TotalMilliseconds:F2}ms");

                return CreateFailureReport(notes, "Pipeline execution failed");
            }
        }

        /// <summary>
        /// Creates a failure report
        /// </summary>
        private static PipelineReport CreateFailureReport(List<string> notes, string reason)
        {
            notes.Add(reason);
            return new PipelineReport(
                $"failure-{Guid.NewGuid():N}",
                false,
                0.0,
                notes
            );
        }
    }
}
