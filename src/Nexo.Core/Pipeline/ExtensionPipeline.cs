using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Contracts;
using Nexo.Core.Configuration;
using Nexo.Observability.ActivitySources;
using Nexo.Observability.Metrics;

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
        private readonly IRepairStrategy<TRequest, TArtifact>? _repairStrategy;
        private readonly ICanaryDeployer<TArtifact>? _canaryDeployer;
        private readonly IRollbackStrategy<TArtifact>? _rollbackStrategy;
        private readonly RepairLoopOptions _repairOptions;
        private readonly ActivitySource _pipelineActivitySource;
        private readonly ActivitySource _generationActivitySource;
        private readonly ActivitySource _validationActivitySource;
        private readonly ActivitySource _policyActivitySource;
        private readonly ActivitySource _repairActivitySource;
        private readonly PipelineMetrics _metrics;

        /// <summary>
        /// Initializes a new instance of the ExtensionPipeline class.
        /// </summary>
        /// <param name="generator">The extension generator</param>
        /// <param name="gates">The compilation gates to validate against</param>
        /// <param name="policies">The policy gates to evaluate against</param>
        /// <param name="publisher">The artifact publisher</param>
        /// <param name="logger">The logger instance</param>
        /// <param name="repairStrategy">Optional repair strategy for failed artifacts</param>
        /// <param name="canaryDeployer">Optional canary deployment strategy</param>
        /// <param name="rollbackStrategy">Optional rollback strategy for failed deployments</param>
        /// <param name="repairOptions">Configuration options for the repair loop</param>
        /// <param name="pipelineActivitySource">Activity source for pipeline operations</param>
        /// <param name="generationActivitySource">Activity source for generation operations</param>
        /// <param name="validationActivitySource">Activity source for validation operations</param>
        /// <param name="policyActivitySource">Activity source for policy operations</param>
        /// <param name="repairActivitySource">Activity source for repair operations</param>
        /// <param name="metrics">Pipeline metrics</param>
        public ExtensionPipeline(
            IExtensionGenerator<TRequest, TArtifact> generator,
            IEnumerable<ICompilationGate> gates,
            IEnumerable<IPolicyGate<TArtifact>> policies,
            IArtifactPublisher<TArtifact> publisher,
            ILogger<ExtensionPipeline<TRequest, TArtifact>> logger,
            IRepairStrategy<TRequest, TArtifact>? repairStrategy = null,
            ICanaryDeployer<TArtifact>? canaryDeployer = null,
            IRollbackStrategy<TArtifact>? rollbackStrategy = null,
            IOptions<RepairLoopOptions>? repairOptions = null,
            ActivitySource? pipelineActivitySource = null,
            ActivitySource? generationActivitySource = null,
            ActivitySource? validationActivitySource = null,
            ActivitySource? policyActivitySource = null,
            ActivitySource? repairActivitySource = null,
            PipelineMetrics? metrics = null)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _compilationGates = gates ?? throw new ArgumentNullException(nameof(gates));
            _policyGates = policies ?? throw new ArgumentNullException(nameof(policies));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repairStrategy = repairStrategy;
            _canaryDeployer = canaryDeployer;
            _rollbackStrategy = rollbackStrategy;
            _repairOptions = repairOptions?.Value ?? new RepairLoopOptions();
            _pipelineActivitySource = pipelineActivitySource ?? NexoActivitySources.Pipeline;
            _generationActivitySource = generationActivitySource ?? NexoActivitySources.Generation;
            _validationActivitySource = validationActivitySource ?? NexoActivitySources.Validation;
            _policyActivitySource = policyActivitySource ?? NexoActivitySources.Policy;
            _repairActivitySource = repairActivitySource ?? NexoActivitySources.Repair;
            _metrics = metrics ?? new PipelineMetrics(new Meter("Nexo.Pipeline"));
        }

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
                _logger.LogInformation("Step 1: Generating artifact");
                notes.Add($"Generation started at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                var generationStopwatch = Stopwatch.StartNew();

                using var generationActivity = _generationActivitySource.StartActivity("generation.execute");
                generationActivity?.SetTag("request.type", typeof(TRequest).Name);
                generationActivity?.SetTag("generation.id", Guid.NewGuid().ToString("N")[..8]);

                try
                {
                    currentGenerationResult = await _generator.GenerateAsync(request, ct);
                    generationActivity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    generationActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    throw;
                }
                finally
                {
                    generationStopwatch.Stop();
                    _metrics.RecordGenerationDuration(generationStopwatch.Elapsed.TotalMilliseconds, "artifact_generation", true);
                }

                notes.Add($"Generation completed in {generationStopwatch.Elapsed.TotalMilliseconds:F2}ms");
                notes.Add($"Source code length: {currentGenerationResult.SourceCode.Length} characters");

                if (currentGenerationResult.Notes.Any())
                {
                    notes.AddRange(currentGenerationResult.Notes.Select(note => $"Generation note: {note}"));
                }

                _logger.LogInformation("Generation completed successfully in {Duration}ms", generationStopwatch.Elapsed.TotalMilliseconds);

                // Step 2: Run validation and repair loop
                var repairAttempts = 0;
                var maxRepairAttempts = _repairOptions.MaxRepairIterations;
                var repairSuccess = false;

                while (repairAttempts <= maxRepairAttempts)
                {
                    _logger.LogInformation("Step 2: Running validation (attempt {Attempt}/{MaxAttempts})", 
                        repairAttempts + 1, maxRepairAttempts + 1);

                    // Run compilation gates
                    validationResults.Clear();
                    var compilationStopwatch = Stopwatch.StartNew();

                    using var validationActivity = _validationActivitySource.StartActivity("validation.compile_gates");
                    validationActivity?.SetTag("gate_count", _compilationGates.Count());
                    validationActivity?.SetTag("attempt", repairAttempts + 1);

                    var validationSuccess = true;
                    foreach (var gate in _compilationGates)
                    {
                        _logger.LogDebug("Running compilation gate: {GateType}", gate.GetType().Name);
                        var validationResult = await gate.ValidateAsync(currentGenerationResult.SourceCode, ct);
                        validationResults.Add(validationResult);

                        if (!validationResult.Passed)
                        {
                            validationSuccess = false;
                            _logger.LogWarning("Compilation gate {GateName} failed with {FindingCount} findings", 
                                validationResult.Name, validationResult.Findings.Count);
                            notes.Add($"Compilation gate '{validationResult.Name}' failed");
                            notes.AddRange(validationResult.Findings.Select(finding => $"Gate failure: {finding}"));
                            _metrics.RecordValidationFailure("compilation_gate", validationResult.Name);
                        }
                        else
                        {
                            _logger.LogDebug("Compilation gate {GateName} passed", validationResult.Name);
                            notes.Add($"Compilation gate '{validationResult.Name}' passed");
                        }
                    }

                    compilationStopwatch.Stop();
                    _metrics.RecordValidationDuration(compilationStopwatch.Elapsed.TotalMilliseconds, "compilation_gates", validationSuccess);
                    validationActivity?.SetStatus(validationSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                    notes.Add($"Compilation gates completed in {compilationStopwatch.Elapsed.TotalMilliseconds:F2}ms");

                    // Check if any compilation gate failed
                    var failedGates = validationResults.Where(vr => !vr.Passed).ToList();
                    if (failedGates.Any())
                    {
                        _logger.LogWarning("Compilation gates failed. Failed gates: {FailedGates}", 
                            string.Join(", ", failedGates.Select(fg => fg.Name)));

                        // Attempt repair if repair strategy is available and we haven't exceeded max attempts
                        if (_repairStrategy != null && repairAttempts < maxRepairAttempts)
                        {
                            _logger.LogInformation("Attempting repair (attempt {Attempt}/{MaxAttempts})", 
                                repairAttempts + 1, maxRepairAttempts);

                            using var repairActivity = _repairActivitySource.StartActivity("repair.attempt");
                            repairActivity?.SetTag("attempt", repairAttempts + 1);
                            repairActivity?.SetTag("max_attempts", maxRepairAttempts);
                            repairActivity?.SetTag("failure_type", "compilation_gates");

                            try
                            {
                                var repairResult = await _repairStrategy.TryRepairAsync(
                                    request, 
                                    currentGenerationResult, 
                                    validationResults, 
                                    null, 
                                    ct);

                                if (repairResult.Applied)
                                {
                                    repairActivity?.SetStatus(ActivityStatusCode.Ok);
                                    _logger.LogInformation("Repair applied successfully: {Note}", repairResult.Note);
                                    notes.Add($"Repair attempt {repairAttempts + 1}: {repairResult.Note}");
                                    
                                    // Update the generation result with repaired source
                                    currentGenerationResult = new GenerationResult<TArtifact>(
                                        currentGenerationResult.Artifact,
                                        repairResult.PatchedSource,
                                        currentGenerationResult.Notes.Concat(new[] { repairResult.Note }).ToList()
                                    );
                                    
                                    repairAttempts++;
                                    continue; // Retry validation with repaired source
                                }
                                else
                                {
                                    repairActivity?.SetStatus(ActivityStatusCode.Error, repairResult.Note);
                                    _logger.LogWarning("Repair attempt {Attempt} failed: {Note}", repairAttempts + 1, repairResult.Note);
                                    notes.Add($"Repair attempt {repairAttempts + 1} failed: {repairResult.Note}");
                                }
                            }
                            catch (Exception repairEx)
                            {
                                repairActivity?.SetStatus(ActivityStatusCode.Error, repairEx.Message);
                                _logger.LogError(repairEx, "Repair attempt {Attempt} threw exception", repairAttempts + 1);
                                notes.Add($"Repair attempt {repairAttempts + 1} failed with exception: {repairEx.Message}");
                            }
                        }

                        // If we get here, either no repair strategy or repair failed
                        var qualityScore = Math.Max(0, 60 - (failedGates.Count * 15));
                        notes.Add($"Pipeline failed due to compilation gate failures. Quality score: {qualityScore}");

                        var failureReport = new PipelineReport(
                            $"artifact-{Guid.NewGuid():N}",
                            false,
                            qualityScore,
                            notes
                        );

                        // Still publish the failure report
                        var publishedReport = await _publisher.PublishAsync(currentGenerationResult.Artifact, validationResults, 
                            new PolicyOutcome(false, "Skipped", new[] { "Compilation gates failed" }, 0), ct);

                        pipelineStopwatch.Stop();
                        _logger.LogInformation("Pipeline completed with failures in {TotalDuration}ms", pipelineStopwatch.Elapsed.TotalMilliseconds);

                        return publishedReport;
                    }

                    // Compilation gates passed, now run policy gates
                    _logger.LogInformation("Step 3: Running policy gates");
                    var policyStopwatch = Stopwatch.StartNew();

                    using var policyActivity = _policyActivitySource.StartActivity("policy.evaluate_gates");
                    policyActivity?.SetTag("policy_count", _policyGates.Count());
                    policyActivity?.SetTag("attempt", repairAttempts + 1);

                    var policyResults = new List<PolicyOutcome>();
                    var policySuccess = true;
                    foreach (var policy in _policyGates)
                    {
                        _logger.LogDebug("Running policy gate: {PolicyType}", policy.GetType().Name);
                        var policyResult = await policy.EvaluateAsync(currentGenerationResult.Artifact, ct);
                        policyResults.Add(policyResult);

                        _metrics.RecordPolicyScore((long)(policyResult.Score * 100), policyResult.PolicyPackName);

                        if (!policyResult.Passed)
                        {
                            policySuccess = false;
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
                    _metrics.RecordPolicyEvaluationDuration(policyStopwatch.Elapsed.TotalMilliseconds, "policy_gates");
                    policyActivity?.SetStatus(policySuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                    notes.Add($"Policy gates completed in {policyStopwatch.Elapsed.TotalMilliseconds:F2}ms");

                    // Aggregate policy results
                    var aggregatedPolicy = AggregatePolicyResults(policyResults);
                    notes.Add($"Aggregated policy result: Passed={aggregatedPolicy.Passed}, Score={aggregatedPolicy.Score:F2}");

                    // Check if policy gates failed and attempt repair
                    if (!aggregatedPolicy.Passed && _repairStrategy != null && repairAttempts < maxRepairAttempts)
                    {
                        _logger.LogInformation("Policy gates failed, attempting repair (attempt {Attempt}/{MaxAttempts})", 
                            repairAttempts + 1, maxRepairAttempts);

                        try
                        {
                            var repairResult = await _repairStrategy.TryRepairAsync(
                                request, 
                                currentGenerationResult, 
                                validationResults, 
                                aggregatedPolicy, 
                                ct);

                            if (repairResult.Applied)
                            {
                                _logger.LogInformation("Repair applied successfully: {Note}", repairResult.Note);
                                notes.Add($"Repair attempt {repairAttempts + 1}: {repairResult.Note}");
                                
                                // Update the generation result with repaired source
                                currentGenerationResult = new GenerationResult<TArtifact>(
                                    currentGenerationResult.Artifact,
                                    repairResult.PatchedSource,
                                    currentGenerationResult.Notes.Concat(new[] { repairResult.Note }).ToList()
                                );
                                
                                repairAttempts++;
                                continue; // Retry validation with repaired source
                            }
                            else
                            {
                                _logger.LogWarning("Repair attempt {Attempt} failed: {Note}", repairAttempts + 1, repairResult.Note);
                                notes.Add($"Repair attempt {repairAttempts + 1} failed: {repairResult.Note}");
                            }
                        }
                        catch (Exception repairEx)
                        {
                            _logger.LogError(repairEx, "Repair attempt {Attempt} threw exception", repairAttempts + 1);
                            notes.Add($"Repair attempt {repairAttempts + 1} failed with exception: {repairEx.Message}");
                        }
                    }

                    // If we get here, either all gates passed or repair failed
                    repairSuccess = true;
                    break;
                }

                if (!repairSuccess)
                {
                    _logger.LogError("Pipeline failed after {MaxAttempts} repair attempts", maxRepairAttempts);
                    notes.Add($"Pipeline failed after {maxRepairAttempts} repair attempts");

                    var failureReport = new PipelineReport(
                        $"artifact-{Guid.NewGuid():N}",
                        false,
                        0.0,
                        notes
                    );

                    return failureReport;
                }

                // Step 4: Canary deployment (if enabled and available)
                if (_repairOptions.EnableCanaryDeployment && _canaryDeployer != null)
                {
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
                        }
                        else
                        {
                            _logger.LogWarning("Canary deployment failed: {Note}", canaryResult.Note);
                            notes.Add($"Canary deployment failed: {canaryResult.Note}");

                            // Attempt rollback if enabled and available
                            if (_repairOptions.EnableAutomaticRollback && _rollbackStrategy != null)
                            {
                                _logger.LogInformation("Attempting rollback due to canary failure");
                                try
                                {
                                    var rollbackResult = await _rollbackStrategy.RollbackAsync(
                                        currentGenerationResult.Artifact, 
                                        "Canary deployment failed", 
                                        ct);
                                    
                                    _logger.LogInformation("Rollback completed: {Result}", rollbackResult);
                                    notes.Add($"Rollback completed: {rollbackResult}");

                                    var rollbackReport = new PipelineReport(
                                        $"rollback-{Guid.NewGuid():N}",
                                        false,
                                        0.0,
                                        notes
                                    );

                                    return rollbackReport;
                                }
                                catch (Exception rollbackEx)
                                {
                                    _logger.LogError(rollbackEx, "Rollback failed");
                                    notes.Add($"Rollback failed with exception: {rollbackEx.Message}");
                                }
                            }

                            // If rollback failed or not available, return failure
                            var canaryFailureReport = new PipelineReport(
                                $"canary-failure-{Guid.NewGuid():N}",
                                false,
                                0.0,
                                notes
                            );

                            return canaryFailureReport;
                        }

                        notes.Add($"Canary deployment completed in {canaryStopwatch.Elapsed.TotalMilliseconds:F2}ms");
                    }
                    catch (Exception canaryEx)
                    {
                        _logger.LogError(canaryEx, "Canary deployment threw exception");
                        notes.Add($"Canary deployment failed with exception: {canaryEx.Message}");

                        // Attempt rollback on canary exception
                        if (_repairOptions.EnableAutomaticRollback && _rollbackStrategy != null)
                        {
                            try
                            {
                                var rollbackResult = await _rollbackStrategy.RollbackAsync(
                                    currentGenerationResult.Artifact, 
                                    "Canary deployment exception", 
                                    ct);
                                
                                _logger.LogInformation("Rollback completed after canary exception: {Result}", rollbackResult);
                                notes.Add($"Rollback completed after canary exception: {rollbackResult}");
                            }
                            catch (Exception rollbackEx)
                            {
                                _logger.LogError(rollbackEx, "Rollback after canary exception failed");
                                notes.Add($"Rollback after canary exception failed: {rollbackEx.Message}");
                            }
                        }

                        var canaryExceptionReport = new PipelineReport(
                            $"canary-exception-{Guid.NewGuid():N}",
                            false,
                            0.0,
                            notes
                        );

                        return canaryExceptionReport;
                    }
                }

                // Step 5: Publish the result
                _logger.LogInformation("Step 5: Publishing result");
                var publishStopwatch = Stopwatch.StartNew();

                var finalReport = await _publisher.PublishAsync(currentGenerationResult.Artifact, validationResults, 
                    new PolicyOutcome(true, "Success", new[] { "All validations passed" }, 1.0), ct);
                publishStopwatch.Stop();

                notes.Add($"Publishing completed in {publishStopwatch.Elapsed.TotalMilliseconds:F2}ms");
                notes.Add($"Final report: Succeeded={finalReport.Succeeded}, QualityScore={finalReport.QualityScore:F2}");

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
