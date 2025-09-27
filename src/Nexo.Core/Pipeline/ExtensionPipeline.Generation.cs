using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Contracts;
using Nexo.Observability.ActivitySources;
using Nexo.Observability.Metrics;

namespace Nexo.Core.Pipeline
{
    /// <summary>
    /// Generation step functionality for ExtensionPipeline.
    /// </summary>
    public sealed partial class ExtensionPipeline<TRequest, TArtifact>
    {
        /// <summary>
        /// Executes the generation step
        /// </summary>
        private async Task<GenerationResult<TArtifact>> ExecuteGenerationStep(TRequest request, List<string> notes, CancellationToken ct)
        {
            _logger.LogInformation("Step 1: Generating artifact");
            notes.Add($"Generation started at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            var generationStopwatch = Stopwatch.StartNew();

            using var generationActivity = _generationActivitySource.StartActivity("generation.execute");
            generationActivity?.SetTag("request.type", typeof(TRequest).Name);
            generationActivity?.SetTag("generation.id", Guid.NewGuid().ToString("N")[..8]);

            try
            {
                var result = await _generator.GenerateAsync(request, ct);
                generationActivity?.SetStatus(ActivityStatusCode.Ok);
                
                generationStopwatch.Stop();
                _metrics.RecordGenerationDuration(generationStopwatch.Elapsed.TotalMilliseconds, "artifact_generation", true);
                
                notes.Add($"Generation completed in {generationStopwatch.Elapsed.TotalMilliseconds:F2}ms");
                notes.Add($"Source code length: {result.SourceCode.Length} characters");

                if (result.Notes.Any())
                {
                    notes.AddRange(result.Notes.Select(note => $"Generation note: {note}"));
                }

                _logger.LogInformation("Generation completed successfully in {Duration}ms", generationStopwatch.Elapsed.TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                generationActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }
}
