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
    /// Step execution methods for WorkflowExecutionService.
    /// </summary>
    public partial class WorkflowExecutionService
    {
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

        private Task ExecuteFileOperationStepAsync(
            WorkflowStep step,
            string projectPath,
            WorkflowStepResult result,
            CancellationToken cancellationToken)
        {
            // Implement file operations (copy, move, delete, etc.)
            // For now, just log the operation
            _logger.LogInformation("File operation: {Command} {Arguments}", step.Command, string.Join(" ", step.Arguments));
            result.ExitCode = 0;
            return Task.CompletedTask;
        }

        private Task ExecuteHttpRequestStepAsync(
            WorkflowStep step,
            string projectPath,
            WorkflowStepResult result,
            CancellationToken cancellationToken)
        {
            // Implement HTTP request execution
            // For now, just log the operation
            _logger.LogInformation("HTTP request: {Command} {Arguments}", step.Command, string.Join(" ", step.Arguments));
            result.ExitCode = 0;
            return Task.CompletedTask;
        }

        private Task ExecuteCustomStepAsync(
            WorkflowStep step,
            string projectPath,
            WorkflowStepResult result,
            CancellationToken cancellationToken)
        {
            // Implement custom step execution
            // For now, just log the operation
            _logger.LogInformation("Custom step: {Command} {Arguments}", step.Command, string.Join(" ", step.Arguments));
            result.ExitCode = 0;
            return Task.CompletedTask;
        }
    }
}
