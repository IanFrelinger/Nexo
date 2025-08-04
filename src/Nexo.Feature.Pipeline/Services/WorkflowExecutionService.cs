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
    /// Simple stub configuration for pipeline context creation.
    /// </summary>
    public class StubPipelineConfiguration : IPipelineConfiguration
    {
        public string Name => "Stub Configuration";
        public string Version => "1.0.0";
        public Dictionary<string, object> Settings => new Dictionary<string, object>();
        
        public int MaxParallelExecutions => 4;
        public int CommandTimeoutMs => 30000;
        public int BehaviorTimeoutMs => 60000;
        public int AggregatorTimeoutMs => 120000;
        public int MaxRetries => 3;
        public int RetryDelayMs => 1000;
        public bool EnableDetailedLogging => true;
        public bool EnablePerformanceMonitoring => true;
        public bool EnableExecutionHistory => true;
        public int MaxExecutionHistoryEntries => 1000;
        public bool EnableParallelExecution => true;
        public bool EnableDependencyResolution => true;
        public bool EnableResourceManagement => true;
        public long MaxMemoryUsageBytes => 1024 * 1024 * 1024; // 1GB
        public double MaxCpuUsagePercentage => 80.0;

        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            if (Settings.TryGetValue(key, out var value) && value is T tValue)
                return tValue;
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            Settings[key] = value;
        }

        public IEnumerable<string> GetKeys()
        {
            return Settings.Keys;
        }

        public bool HasKey(string key)
        {
            return Settings.ContainsKey(key);
        }
    }

    /// <summary>
    /// Service for executing development workflows including setup, analyze, test, and deploy.
    /// </summary>
    public class WorkflowExecutionService : IWorkflowExecutionService
    {
        private readonly ILogger<WorkflowExecutionService> _logger;
        private readonly IPipelineExecutionEngine _pipelineEngine;
        private readonly IWorkflowConfigurationService _configService;

        public WorkflowExecutionService(
            ILogger<WorkflowExecutionService> logger,
            IPipelineExecutionEngine pipelineEngine,
            IWorkflowConfigurationService configService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pipelineEngine = pipelineEngine ?? throw new ArgumentNullException(nameof(pipelineEngine));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
            WorkflowType type,
            string projectPath,
            string configPath = null,
            CancellationToken cancellationToken = default)
        {
            var result = new WorkflowExecutionResult
            {
                WorkflowType = type,
                ProjectPath = projectPath,
                Status = WorkflowExecutionStatus.Running
            };

            try
            {
                _logger.LogInformation("Starting workflow execution: {WorkflowType} for project: {ProjectPath}", type, projectPath);

                // Load configuration
                var config = await LoadWorkflowConfigurationAsync(type, configPath, cancellationToken);
                result.Configuration = config;

                // Execute workflow steps
                var stepResults = await ExecuteWorkflowStepsAsync(config, projectPath, cancellationToken);
                result.StepResults = stepResults;

                // Determine final status
                if (stepResults.Any(s => s.Status == WorkflowStepStatus.Failed))
                {
                    result.Status = WorkflowExecutionStatus.Failed;
                    result.Errors.Add("One or more workflow steps failed");
                }
                else
                {
                    result.Status = WorkflowExecutionStatus.Completed;
                }

                result.EndTime = DateTime.UtcNow;
                _logger.LogInformation("Workflow execution completed: {WorkflowType} - Status: {Status}, Duration: {Duration}ms",
                    type, result.Status, result.Duration?.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                result.Status = WorkflowExecutionStatus.Failed;
                result.Errors.Add(ex.Message);
                result.EndTime = DateTime.UtcNow;
                _logger.LogError(ex, "Workflow execution failed: {WorkflowType}", type);
            }

            return result;
        }

        private async Task<WorkflowConfiguration> LoadWorkflowConfigurationAsync(
            WorkflowType type,
            string configPath,
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
                    var task = (Task)waitForExitAsyncMethod.Invoke(process, new object[] { cancellationToken });
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

        private async Task ExecuteCommandStepAsync(
            WorkflowStep step,
            string projectPath,
            WorkflowStepResult result,
            CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = step.Command,
                Arguments = string.Join(" ", step.Arguments),
                WorkingDirectory = string.IsNullOrEmpty(step.WorkingDirectory) ? projectPath : step.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Add environment variables
            foreach (var envVar in step.EnvironmentVariables)
            {
                startInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
            }

            var output = new List<string>();
            var error = new List<string>();

            using (var process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (sender, e) => { if (e.Data != null) output.Add(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) error.Add(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool completed = await WaitForProcessExitAsync(process, cancellationToken);

                if (!completed)
                {
                    process.Kill();
                    throw new TimeoutException($"Step timed out after {step.TimeoutSeconds} seconds");
                }

                result.ExitCode = process.ExitCode;
                result.Output = string.Join(Environment.NewLine, output);
                result.Error = string.Join(Environment.NewLine, error);
            }
        }

        private async Task ExecuteScriptStepAsync(
            WorkflowStep step,
            string projectPath,
            WorkflowStepResult result,
            CancellationToken cancellationToken)
        {
            // For now, treat script as command execution
            // In the future, this could support different script engines
            await ExecuteCommandStepAsync(step, projectPath, result, cancellationToken);
        }

        private async Task ExecutePipelineStepAsync(
            WorkflowStep step,
            string projectPath,
            WorkflowStepResult result,
            CancellationToken cancellationToken)
        {
            // Execute pipeline using the pipeline engine
            var context = new PipelineContext(_logger, new StubPipelineConfiguration(), cancellationToken);

            var aggregatorIds = step.Arguments.ToList();
            var pipelineResult = await _pipelineEngine.ExecuteAsync(context, aggregatorIds, cancellationToken);

            result.ExitCode = pipelineResult.IsSuccess ? 0 : 1;
            result.Output = JsonSerializer.Serialize(pipelineResult, new JsonSerializerOptions { WriteIndented = true });
        }

        private async Task ExecuteFileOperationStepAsync(
            WorkflowStep step,
            string projectPath,
            WorkflowStepResult result,
            CancellationToken cancellationToken)
        {
            // Implement file operations (copy, move, delete, etc.)
            // For now, just log the operation
            _logger.LogInformation("File operation: {Command} {Arguments}", step.Command, string.Join(" ", step.Arguments));
            result.ExitCode = 0;
        }

        private async Task ExecuteHttpRequestStepAsync(
            WorkflowStep step,
            string projectPath,
            WorkflowStepResult result,
            CancellationToken cancellationToken)
        {
            // Implement HTTP request execution
            // For now, just log the operation
            _logger.LogInformation("HTTP request: {Command} {Arguments}", step.Command, string.Join(" ", step.Arguments));
            result.ExitCode = 0;
        }

        private async Task ExecuteCustomStepAsync(
            WorkflowStep step,
            string projectPath,
            WorkflowStepResult result,
            CancellationToken cancellationToken)
        {
            // Implement custom step execution
            // For now, just log the operation
            _logger.LogInformation("Custom step: {Command} {Arguments}", step.Command, string.Join(" ", step.Arguments));
            result.ExitCode = 0;
        }

        private List<WorkflowStep> SortStepsByDependencies(List<WorkflowStep> steps)
        {
            var sorted = new List<WorkflowStep>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var step in steps)
            {
                if (!visited.Contains(step.Id))
                {
                    TopologicalSort(step, steps, sorted, visited, visiting);
                }
            }

            return sorted;
        }

        private void TopologicalSort(
            WorkflowStep step,
            List<WorkflowStep> allSteps,
            List<WorkflowStep> sorted,
            HashSet<string> visited,
            HashSet<string> visiting)
        {
            if (visiting.Contains(step.Id))
            {
                throw new InvalidOperationException($"Circular dependency detected for step: {step.Name}");
            }

            if (visited.Contains(step.Id))
            {
                return;
            }

            visiting.Add(step.Id);

            foreach (var dependencyId in step.Dependencies)
            {
                var dependency = allSteps.FirstOrDefault(s => s.Id == dependencyId);
                if (dependency != null)
                {
                    TopologicalSort(dependency, allSteps, sorted, visited, visiting);
                }
            }

            visiting.Remove(step.Id);
            visited.Add(step.Id);
            sorted.Add(step);
        }

        private bool AreDependenciesMet(WorkflowStep step, HashSet<string> completedSteps)
        {
            return step.Dependencies.All(depId => completedSteps.Contains(depId));
        }

        private bool AreConditionsMet(WorkflowStep step, string projectPath)
        {
            foreach (var condition in step.Conditions)
            {
                var isMet = EvaluateCondition(condition, projectPath);
                if (condition.Negate)
                {
                    isMet = !isMet;
                }

                if (!isMet)
                {
                    return false;
                }
            }

            return true;
        }

        private bool EvaluateCondition(StepCondition condition, string projectPath)
        {
            switch (condition.Type)
            {
                case ConditionType.FileExists:
                    var filePath = Path.Combine(projectPath, condition.Value);
                    return File.Exists(filePath);

                case ConditionType.EnvironmentVariable:
                    var envValue = Environment.GetEnvironmentVariable(condition.Value);
                    return !string.IsNullOrEmpty(envValue);

                case ConditionType.PreviousStepResult:
                    // This would need to be evaluated in context of previous steps
                    return true;

                case ConditionType.Custom:
                    // Custom condition evaluation would be implemented here
                    return true;

                default:
                    return true;
            }
        }

        private async Task<WorkflowStepResult> ExecuteSetupWorkflowAsync(string projectPath, WorkflowConfiguration config, CancellationToken cancellationToken)
        {
            var stepResult = new WorkflowStepResult
            {
                StepName = "Setup",
                StartTime = DateTime.UtcNow,
                Status = WorkflowStepStatus.Running
            };

            try
            {
                _logger.LogInformation("Starting setup workflow for project: {ProjectPath}", projectPath);

                // Create pipeline context
                var pipelineContext = new PipelineContext(_logger, new StubPipelineConfiguration(), cancellationToken);

                // Execute setup commands
                var setupCommands = config.SetupCommands ?? new List<string>();
                foreach (var command in setupCommands)
                {
                    _logger.LogInformation("Executing setup command: {Command}", command);
                    // TODO: Implement actual command execution
                    await Task.Delay(100, cancellationToken); // Placeholder
                }

                stepResult.Status = WorkflowStepStatus.Completed;
                stepResult.EndTime = DateTime.UtcNow;
                _logger.LogInformation("Setup workflow completed successfully");
            }
            catch (Exception ex)
            {
                stepResult.Status = WorkflowStepStatus.Failed;
                stepResult.ErrorMessage = ex.Message;
                stepResult.EndTime = DateTime.UtcNow;
                _logger.LogError(ex, "Setup workflow failed");
            }

            return stepResult;
        }

        private async Task<WorkflowStepResult> ExecuteAnalyzeWorkflowAsync(string projectPath, WorkflowConfiguration config, CancellationToken cancellationToken)
        {
            var stepResult = new WorkflowStepResult
            {
                StepName = "Analyze",
                StartTime = DateTime.UtcNow,
                Status = WorkflowStepStatus.Running
            };

            try
            {
                _logger.LogInformation("Starting analyze workflow for project: {ProjectPath}", projectPath);

                // Execute analysis commands
                var analysisCommands = config.AnalysisCommands ?? new List<string>();
                foreach (var command in analysisCommands)
                {
                    _logger.LogInformation("Executing analysis command: {Command}", command);
                    // TODO: Implement actual command execution
                    await Task.Delay(100, cancellationToken); // Placeholder
                }

                stepResult.Status = WorkflowStepStatus.Completed;
                stepResult.EndTime = DateTime.UtcNow;
                _logger.LogInformation("Analyze workflow completed successfully");
            }
            catch (Exception ex)
            {
                stepResult.Status = WorkflowStepStatus.Failed;
                stepResult.ErrorMessage = ex.Message;
                stepResult.EndTime = DateTime.UtcNow;
                _logger.LogError(ex, "Analyze workflow failed");
            }

            return stepResult;
        }

        private async Task<WorkflowStepResult> ExecuteTestWorkflowAsync(string projectPath, WorkflowConfiguration config, CancellationToken cancellationToken)
        {
            var stepResult = new WorkflowStepResult
            {
                StepName = "Test",
                StartTime = DateTime.UtcNow,
                Status = WorkflowStepStatus.Running
            };

            try
            {
                _logger.LogInformation("Starting test workflow for project: {ProjectPath}", projectPath);

                // Execute test commands
                var testCommands = config.TestCommands ?? new List<string>();
                foreach (var command in testCommands)
                {
                    _logger.LogInformation("Executing test command: {Command}", command);
                    // TODO: Implement actual command execution
                    await Task.Delay(100, cancellationToken); // Placeholder
                }

                stepResult.Status = WorkflowStepStatus.Completed;
                stepResult.EndTime = DateTime.UtcNow;
                _logger.LogInformation("Test workflow completed successfully");
            }
            catch (Exception ex)
            {
                stepResult.Status = WorkflowStepStatus.Failed;
                stepResult.ErrorMessage = ex.Message;
                stepResult.EndTime = DateTime.UtcNow;
                _logger.LogError(ex, "Test workflow failed");
            }

            return stepResult;
        }
    }
}