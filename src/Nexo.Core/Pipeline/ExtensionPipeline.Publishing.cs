using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Contracts;

namespace Nexo.Core.Pipeline
{
    /// <summary>
    /// Publishing functionality for ExtensionPipeline.
    /// </summary>
    public sealed partial class ExtensionPipeline<TRequest, TArtifact>
    {
        /// <summary>
        /// Executes the publishing step
        /// </summary>
        private async Task<PipelineReport> ExecutePublishingStep(
            GenerationResult<TArtifact> currentGenerationResult,
            List<ValidationResult> validationResults,
            List<string> notes,
            CancellationToken ct)
        {
            _logger.LogInformation("Step 5: Publishing result");
            var publishStopwatch = Stopwatch.StartNew();

            var finalReport = await _publisher.PublishAsync(currentGenerationResult.Artifact, validationResults, 
                new PolicyOutcome(true, "Success", new[] { "All validations passed" }, 1.0), ct);
            
            publishStopwatch.Stop();

            notes.Add($"Publishing completed in {publishStopwatch.Elapsed.TotalMilliseconds:F2}ms");
            notes.Add($"Final report: Succeeded={finalReport.Succeeded}, QualityScore={finalReport.QualityScore:F2}");

            return finalReport;
        }
    }
}
