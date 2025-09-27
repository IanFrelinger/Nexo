using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Core workflow execution methods for WorkflowExecutionService.
    /// </summary>
    public partial class WorkflowExecutionService
    {
        private async Task<WorkflowConfiguration> LoadWorkflowConfigurationAsync(
            WorkflowType type,
            string? configPath,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(configPath))
            {
                return await _configService.LoadFromFileAsync(configPath, cancellationToken);
            }

            // Load default configuration for the workflow type
            return await _configService.GetDefaultConfigurationAsync(type, cancellationToken);
        }

        private async Task<List<WorkflowStepResult>> ExecuteWorkflowStepsAsync(
            WorkflowConfiguration config,
            string projectPath,
            CancellationToken cancellationToken)
        {
            var results = new List<WorkflowStepResult>();
            var completedSteps = new HashSet<string>();

            // Sort steps by dependencies
            var sortedSteps = SortStepsByDependencies(config.Steps);

            foreach (var step in sortedSteps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Workflow execution cancelled");
                    break;
                }

                var stepResult = await ExecuteStepAsync(step, projectPath, config, completedSteps, cancellationToken);
                results.Add(stepResult);

                if (stepResult.IsSuccess)
                {
                    completedSteps.Add(step.Id);
                }
                else if (step.IsRequired && !config.ContinueOnFailure)
                {
                    _logger.LogError("Required step failed: {StepName}", step.Name);
                    break;
                }
            }

            return results;
        }

        private async Task<WorkflowStepResult> ExecuteStepAsync(
            WorkflowStep step,
            string projectPath,
            WorkflowConfiguration config,
            HashSet<string> completedSteps,
            CancellationToken cancellationToken)
        {
            var stepResult = new WorkflowStepResult
            {
                Step = step,
                StartTime = DateTime.UtcNow,
                Status = WorkflowStepStatus.Running
            };

            try
            {
                _logger.LogInformation("Executing step: {StepName}", step.Name);

                // Check dependencies
                if (!AreDependenciesMet(step, completedSteps))
                {
                    stepResult.Status = WorkflowStepStatus.Skipped;
                    stepResult.ErrorMessage = "Step dependencies not met";
                    return stepResult;
                }

                // Check conditions
                if (!AreConditionsMet(step, projectPath))
                {
                    stepResult.Status = WorkflowStepStatus.Skipped;
                    stepResult.ErrorMessage = "Step conditions not met";
                    return stepResult;
                }

                // Execute step based on type
                switch (step.Type)
                {
                    case StepType.Command:
                        await ExecuteCommandStepAsync(step, projectPath, stepResult, cancellationToken);
                        break;
                    case StepType.Script:
                        await ExecuteScriptStepAsync(step, projectPath, stepResult, cancellationToken);
                        break;
                    case StepType.Pipeline:
                        await ExecutePipelineStepAsync(step, projectPath, stepResult, cancellationToken);
                        break;
                    case StepType.FileOperation:
                        await ExecuteFileOperationStepAsync(step, projectPath, stepResult, cancellationToken);
                        break;
                    case StepType.HttpRequest:
                        await ExecuteHttpRequestStepAsync(step, projectPath, stepResult, cancellationToken);
                        break;
                    case StepType.Custom:
                        await ExecuteCustomStepAsync(step, projectPath, stepResult, cancellationToken);
                        break;
                    default:
                        throw new NotSupportedException($"Step type {step.Type} is not supported");
                }

                stepResult.EndTime = DateTime.UtcNow;
                stepResult.Duration = stepResult.EndTime - stepResult.StartTime;

                if (stepResult.ExitCode.HasValue && step.ExpectedExitCodes.Contains(stepResult.ExitCode.Value))
                {
                    stepResult.Status = WorkflowStepStatus.Completed;
                }
                else
                {
                    stepResult.Status = WorkflowStepStatus.Failed;
                }

                _logger.LogInformation("Step completed: {StepName} - Status: {Status}, Duration: {Duration}ms",
                    step.Name, stepResult.Status, stepResult.Duration.TotalMilliseconds);

                return stepResult;
            }
            catch (Exception ex)
            {
                stepResult.Status = WorkflowStepStatus.Failed;
                stepResult.ErrorMessage = ex.Message;
                stepResult.EndTime = DateTime.UtcNow;
                stepResult.Duration = stepResult.EndTime - stepResult.StartTime;

                _logger.LogError(ex, "Step execution failed: {StepName}", step.Name);
                return stepResult;
            }
        }

        private async Task<bool> WaitForProcessExitAsync(Process process, CancellationToken cancellationToken)
        {
            try
            {
                // Try to use WaitForExitAsync if available (NET Core 2.1+)
                var waitForExitAsyncMethod = typeof(Process).GetMethod("WaitForExitAsync", new[] { typeof(CancellationToken) });
                if (waitForExitAsyncMethod != null)
                {
                    var task = (Task?)waitForExitAsyncMethod.Invoke(process, new object[] { cancellationToken });
                    if (task != null)
                        await task;
                    return true;
                }
            }
            catch (Exception)
            {
                // Fall through to synchronous version
            }

            // Fallback to synchronous WaitForExit
            try
            {
                process.WaitForExit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
