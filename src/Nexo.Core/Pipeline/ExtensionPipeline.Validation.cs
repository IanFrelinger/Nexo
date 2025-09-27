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
    /// Validation and repair functionality for ExtensionPipeline.
    /// </summary>
    public sealed partial class ExtensionPipeline<TRequest, TArtifact>
    {
        /// <summary>
        /// Executes validation and repair loop
        /// </summary>
        private async Task<bool> ExecuteValidationAndRepairLoop(
            TRequest request,
            GenerationResult<TArtifact> currentGenerationResult,
            List<ValidationResult> validationResults,
            List<string> notes,
            CancellationToken ct)
        {
            var repairAttempts = 0;
            var maxRepairAttempts = _repairOptions.MaxRepairIterations;

            while (repairAttempts <= maxRepairAttempts)
            {
                _logger.LogInformation("Step 2: Running validation (attempt {Attempt}/{MaxAttempts})", 
                    repairAttempts + 1, maxRepairAttempts + 1);

                // Run compilation gates
                var compilationSuccess = await ExecuteCompilationGates(currentGenerationResult, validationResults, notes, ct);
                
                if (!compilationSuccess)
                {
                    // Attempt repair if available
                    if (_repairStrategy != null && repairAttempts < maxRepairAttempts)
                    {
                        var repairResult = await AttemptRepair(request, currentGenerationResult, validationResults, null, notes, ct);
                        if (repairResult != null)
                        {
                            currentGenerationResult = repairResult;
                            repairAttempts++;
                            continue;
                        }
                    }
                    return false;
                }

                // Run policy gates
                var policySuccess = await ExecutePolicyGates(currentGenerationResult, notes, ct);
                
                if (!policySuccess && _repairStrategy != null && repairAttempts < maxRepairAttempts)
                {
                    var repairResult = await AttemptRepair(request, currentGenerationResult, validationResults, null, notes, ct);
                    if (repairResult != null)
                    {
                        currentGenerationResult = repairResult;
                        repairAttempts++;
                        continue;
                    }
                }

                return policySuccess;
            }

            _logger.LogError("Pipeline failed after {MaxAttempts} repair attempts", maxRepairAttempts);
            notes.Add($"Pipeline failed after {maxRepairAttempts} repair attempts");
            return false;
        }

        /// <summary>
        /// Executes compilation gates
        /// </summary>
        private async Task<bool> ExecuteCompilationGates(
            GenerationResult<TArtifact> currentGenerationResult,
            List<ValidationResult> validationResults,
            List<string> notes,
            CancellationToken ct)
        {
            validationResults.Clear();
            var compilationStopwatch = Stopwatch.StartNew();

            using var validationActivity = _validationActivitySource.StartActivity("validation.compile_gates");
            validationActivity?.SetTag("gate_count", _compilationGates.Count());

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

            return validationSuccess;
        }

        /// <summary>
        /// Executes policy gates
        /// </summary>
        private async Task<bool> ExecutePolicyGates(GenerationResult<TArtifact> currentGenerationResult, List<string> notes, CancellationToken ct)
        {
            _logger.LogInformation("Step 3: Running policy gates");
            var policyStopwatch = Stopwatch.StartNew();

            using var policyActivity = _policyActivitySource.StartActivity("policy.evaluate_gates");
            policyActivity?.SetTag("policy_count", _policyGates.Count());

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

            return policySuccess;
        }

        /// <summary>
        /// Attempts repair on failed validation
        /// </summary>
        private async Task<GenerationResult<TArtifact>?> AttemptRepair(
            TRequest request,
            GenerationResult<TArtifact> currentGenerationResult,
            List<ValidationResult> validationResults,
            PolicyOutcome? policyOutcome,
            List<string> notes,
            CancellationToken ct)
        {
            if (_repairStrategy == null) return null;

            _logger.LogInformation("Attempting repair");

            using var repairActivity = _repairActivitySource.StartActivity("repair.attempt");
            repairActivity?.SetTag("failure_type", policyOutcome == null ? "compilation_gates" : "policy_gates");

            try
            {
                var repairResult = await _repairStrategy.TryRepairAsync(
                    request,
                    currentGenerationResult,
                    validationResults,
                    policyOutcome,
                    ct);

                if (repairResult.Applied)
                {
                    repairActivity?.SetStatus(ActivityStatusCode.Ok);
                    _logger.LogInformation("Repair applied successfully: {Note}", repairResult.Note);
                    notes.Add($"Repair applied: {repairResult.Note}");

                    return new GenerationResult<TArtifact>(
                        currentGenerationResult.Artifact,
                        repairResult.PatchedSource,
                        currentGenerationResult.Notes.Concat(new[] { repairResult.Note }).ToList()
                    );
                }
                else
                {
                    repairActivity?.SetStatus(ActivityStatusCode.Error, repairResult.Note);
                    _logger.LogWarning("Repair attempt failed: {Note}", repairResult.Note);
                    notes.Add($"Repair attempt failed: {repairResult.Note}");
                }
            }
            catch (Exception repairEx)
            {
                repairActivity?.SetStatus(ActivityStatusCode.Error, repairEx.Message);
                _logger.LogError(repairEx, "Repair attempt threw exception");
                notes.Add($"Repair attempt failed with exception: {repairEx.Message}");
            }

            return null;
        }
    }
}
