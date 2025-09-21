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
    /// Typed pipeline orchestrator that composes generator, compilation gates, policy gates, and publishing.
    /// </summary>
    /// <typeparam name="TRequest">The type of request for generation</typeparam>
    /// <typeparam name="TArtifact">The type of artifact generated</typeparam>
    public sealed class ExtensionPipeline<TRequest, TArtifact>
    {
        private readonly IExtensionGenerator<TRequest, TArtifact> _generator;
        private readonly IEnumerable<ICompilationGate> _compilationGates;
        private readonly IEnumerable<IPolicyGate<TArtifact>> _policyGates;
        private readonly IArtifactPublisher<TArtifact> _publisher;
        private readonly ILogger<ExtensionPipeline<TRequest, TArtifact>> _logger;

        /// <summary>
        /// Initializes a new instance of the ExtensionPipeline class.
        /// </summary>
        /// <param name="generator">The extension generator</param>
        /// <param name="gates">The compilation gates to validate against</param>
        /// <param name="policies">The policy gates to evaluate against</param>
        /// <param name="publisher">The artifact publisher</param>
        /// <param name="logger">The logger instance</param>
        public ExtensionPipeline(
            IExtensionGenerator<TRequest, TArtifact> generator,
            IEnumerable<ICompilationGate> gates,
            IEnumerable<IPolicyGate<TArtifact>> policies,
            IArtifactPublisher<TArtifact> publisher,
            ILogger<ExtensionPipeline<TRequest, TArtifact>> logger)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _compilationGates = gates ?? throw new ArgumentNullException(nameof(gates));
            _policyGates = policies ?? throw new ArgumentNullException(nameof(policies));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs the complete pipeline for the given request.
        /// </summary>
        /// <param name="request">The request to process</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The pipeline report</returns>
        public async Task<PipelineReport> RunAsync(TRequest request, CancellationToken ct = default)
        {
            var pipelineStopwatch = Stopwatch.StartNew();
            var notes = new List<string>();
            var validationResults = new List<ValidationResult>();

            try
            {
                _logger.LogInformation("Starting pipeline execution for request type {RequestType}", typeof(TRequest).Name);

                // Step 1: Generate the artifact
                _logger.LogInformation("Step 1: Generating artifact");
                notes.Add($"Generation started at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                var generationStopwatch = Stopwatch.StartNew();

                var generationResult = await _generator.GenerateAsync(request, ct);
                generationStopwatch.Stop();

                notes.Add($"Generation completed in {generationStopwatch.Elapsed.TotalMilliseconds:F2}ms");
                notes.Add($"Source code length: {generationResult.SourceCode.Length} characters");

                if (generationResult.Notes.Any())
                {
                    notes.AddRange(generationResult.Notes.Select(note => $"Generation note: {note}"));
                }

                _logger.LogInformation("Generation completed successfully in {Duration}ms", generationStopwatch.Elapsed.TotalMilliseconds);

                // Step 2: Run all compilation gates
                _logger.LogInformation("Step 2: Running compilation gates");
                var compilationStopwatch = Stopwatch.StartNew();

                foreach (var gate in _compilationGates)
                {
                    _logger.LogDebug("Running compilation gate: {GateType}", gate.GetType().Name);
                    var validationResult = await gate.ValidateAsync(generationResult.SourceCode, ct);
                    validationResults.Add(validationResult);

                    if (!validationResult.Passed)
                    {
                        _logger.LogWarning("Compilation gate {GateName} failed with {FindingCount} findings", 
                            validationResult.Name, validationResult.Findings.Count);
                        notes.Add($"Compilation gate '{validationResult.Name}' failed");
                        notes.AddRange(validationResult.Findings.Select(finding => $"Gate failure: {finding}"));
                    }
                    else
                    {
                        _logger.LogDebug("Compilation gate {GateName} passed", validationResult.Name);
                        notes.Add($"Compilation gate '{validationResult.Name}' passed");
                    }
                }

                compilationStopwatch.Stop();
                notes.Add($"Compilation gates completed in {compilationStopwatch.Elapsed.TotalMilliseconds:F2}ms");

                // Step 3: Check if any compilation gate failed
                var failedGates = validationResults.Where(vr => !vr.Passed).ToList();
                if (failedGates.Any())
                {
                    _logger.LogWarning("Compilation gates failed, skipping policy evaluation. Failed gates: {FailedGates}", 
                        string.Join(", ", failedGates.Select(fg => fg.Name)));

                    // Calculate quality score based on gate failures (0-60 range)
                    var qualityScore = Math.Max(0, 60 - (failedGates.Count * 15));
                    notes.Add($"Pipeline failed due to compilation gate failures. Quality score: {qualityScore}");

                    var failureReport = new PipelineReport(
                        $"artifact-{Guid.NewGuid():N}",
                        false,
                        qualityScore,
                        notes
                    );

                    // Still publish the failure report
                    var publishedReport = await _publisher.PublishAsync(generationResult.Artifact, validationResults, 
                        new PolicyOutcome(false, "Skipped", new[] { "Compilation gates failed" }, 0), ct);

                    pipelineStopwatch.Stop();
                    _logger.LogInformation("Pipeline completed with failures in {TotalDuration}ms", pipelineStopwatch.Elapsed.TotalMilliseconds);

                    return publishedReport;
                }

                // Step 4: Run policy gates
                _logger.LogInformation("Step 3: Running policy gates");
                var policyStopwatch = Stopwatch.StartNew();

                var policyResults = new List<PolicyOutcome>();
                foreach (var policy in _policyGates)
                {
                    _logger.LogDebug("Running policy gate: {PolicyType}", policy.GetType().Name);
                    var policyResult = await policy.EvaluateAsync(generationResult.Artifact, ct);
                    policyResults.Add(policyResult);

                    if (!policyResult.Passed)
                    {
                        _logger.LogWarning("Policy gate {PolicyName} failed with score {Score}", 
                            policyResult.PolicyPackName, policyResult.Score);
                        notes.Add($"Policy gate '{policyResult.PolicyPackName}' failed (score: {policyResult.Score:F2})");
                        notes.AddRange(policyResult.Findings.Select(finding => $"Policy failure: {finding}"));
                    }
                    else
                    {
                        _logger.LogDebug("Policy gate {PolicyName} passed with score {Score}", 
                            policyResult.PolicyPackName, policyResult.Score);
                        notes.Add($"Policy gate '{policyResult.PolicyPackName}' passed (score: {policyResult.Score:F2})");
                    }
                }

                policyStopwatch.Stop();
                notes.Add($"Policy gates completed in {policyStopwatch.Elapsed.TotalMilliseconds:F2}ms");

                // Step 5: Aggregate policy results
                var aggregatedPolicy = AggregatePolicyResults(policyResults);
                notes.Add($"Aggregated policy result: Passed={aggregatedPolicy.Passed}, Score={aggregatedPolicy.Score:F2}");

                // Step 6: Publish the result
                _logger.LogInformation("Step 4: Publishing result");
                var publishStopwatch = Stopwatch.StartNew();

                var finalReport = await _publisher.PublishAsync(generationResult.Artifact, validationResults, aggregatedPolicy, ct);
                publishStopwatch.Stop();

                notes.Add($"Publishing completed in {publishStopwatch.Elapsed.TotalMilliseconds:F2}ms");
                notes.Add($"Final report: Succeeded={finalReport.Succeeded}, QualityScore={finalReport.QualityScore:F2}");

                pipelineStopwatch.Stop();
                _logger.LogInformation("Pipeline completed successfully in {TotalDuration}ms", pipelineStopwatch.Elapsed.TotalMilliseconds);

                return finalReport;
            }
            catch (Exception ex)
            {
                pipelineStopwatch.Stop();
                _logger.LogError(ex, "Pipeline execution failed after {Duration}ms", pipelineStopwatch.Elapsed.TotalMilliseconds);

                notes.Add($"Pipeline failed with exception: {ex.Message}");
                notes.Add($"Pipeline duration: {pipelineStopwatch.Elapsed.TotalMilliseconds:F2}ms");

                var errorReport = new PipelineReport(
                    $"error-{Guid.NewGuid():N}",
                    false,
                    0.0,
                    notes
                );

                return errorReport;
            }
        }

        /// <summary>
        /// Aggregates multiple policy results into a single outcome.
        /// </summary>
        /// <param name="policyResults">The policy results to aggregate</param>
        /// <returns>The aggregated policy outcome</returns>
        private static PolicyOutcome AggregatePolicyResults(IEnumerable<PolicyOutcome> policyResults)
        {
            var results = policyResults.ToList();
            if (!results.Any())
            {
                return new PolicyOutcome(true, "No Policies", new[] { "No policy gates configured" }, 1.0);
            }

            var allPassed = results.All(r => r.Passed);
            var averageScore = results.Average(r => r.Score);
            var allFindings = results.SelectMany(r => r.Findings).ToList();
            var policyNames = string.Join(", ", results.Select(r => r.PolicyPackName));

            return new PolicyOutcome(allPassed, policyNames, allFindings, averageScore);
        }
    }
}
