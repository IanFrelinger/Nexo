using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Infrastructure.Testing;

/// <summary>
/// Unity-specific infrastructure test runner that executes framework tests via Unity test script.
/// 
/// Integrates with the existing Unity test script (test-framework-unity.sh) to run:
/// - Base Framework Smoke Tests
/// - Infrastructure Stress Tests
/// - Unity compatibility tests
/// 
/// Part of the Infrastructure layer, providing Unity test execution via test runner infrastructure.
/// </summary>
public class UnityInfrastructureTestRunner
{
    private readonly ILogger<UnityInfrastructureTestRunner> _logger;
    private readonly string _projectRoot;

    public UnityInfrastructureTestRunner(ILogger<UnityInfrastructureTestRunner> logger, string projectRoot)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _projectRoot = projectRoot ?? throw new ArgumentNullException(nameof(projectRoot));
    }

    /// <summary>
    /// Runs infrastructure tests in Unity environment using the Unity test script.
    /// </summary>
    public async Task<TestExecutionResult> RunInfrastructureTestsAsync(
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running infrastructure tests in Unity environment");

        progress?.Report(new ProgressReport
        {
            Percentage = 0,
            Message = "Starting Unity infrastructure tests...",
            CurrentStep = 0,
            TotalSteps = 1
        });

        var results = new List<TestResult>();
        var startTime = DateTime.UtcNow;

        // Run Unity test script
        var scriptPath = Path.Combine(_projectRoot, "scripts", "test-framework-unity.sh");
        if (!File.Exists(scriptPath))
        {
            return new TestExecutionResult
            {
                TotalTests = 1,
                PassedTests = 0,
                FailedTests = 1,
                TotalDuration = TimeSpan.Zero,
                Results = new[]
                {
                    new TestResult
                    {
                        Name = "UnityInfrastructureTests",
                        Category = "Unity",
                        Passed = false,
                        Message = $"Unity test script not found: {scriptPath}",
                        Duration = TimeSpan.Zero
                    }
                }
            };
        }

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = _projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            // Make script executable
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var chmodProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{scriptPath}\"",
                    UseShellExecute = false
                });
                chmodProcess?.WaitForExit();
            }

            progress?.Report(new ProgressReport
            {
                Percentage = 50,
                Message = "Executing Unity test script...",
                CurrentStep = 1,
                TotalSteps = 1
            });

            var result = await RunProcessAsync(processInfo, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            // Parse test results from script output
            var output = result.StandardOutput + result.StandardError;
            var (passed, failed, total) = ParseTestResults(output);

            // Create test result
            var testResult = new TestResult
            {
                Name = "UnityInfrastructureTests",
                Category = "Unity",
                Passed = result.ExitCode == 0 && failed == 0,
                Message = $"Unity infrastructure tests: {total} total, {passed} passed, {failed} failed",
                Duration = duration,
                ErrorMessage = result.ExitCode != 0 ? result.StandardError : null
            };

            results.Add(testResult);

            progress?.Report(new ProgressReport
            {
                Percentage = 100,
                Message = $"Unity tests completed: {passed}/{total} passed",
                CurrentStep = 1,
                TotalSteps = 1
            });

            return new TestExecutionResult
            {
                TotalTests = total > 0 ? total : 1,
                PassedTests = passed,
                FailedTests = failed,
                TotalDuration = duration,
                Results = results
            };
        }
        catch (Exception ex)
        {
            return new TestExecutionResult
            {
                TotalTests = 1,
                PassedTests = 0,
                FailedTests = 1,
                TotalDuration = DateTime.UtcNow - startTime,
                Results = new[]
                {
                    new TestResult
                    {
                        Name = "UnityInfrastructureTests",
                        Category = "Unity",
                        Passed = false,
                        Message = $"Error running Unity tests: {ex.Message}",
                        Duration = TimeSpan.Zero,
                        ErrorMessage = ex.ToString()
                    }
                }
            };
        }
    }

    private (int passed, int failed, int total) ParseTestResults(string output)
    {
        // Try to extract test counts from output
        var passedMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\s+passed", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var failedMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\s+failed", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var totalMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\s+total", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var passed = passedMatch.Success ? int.Parse(passedMatch.Groups[1].Value) : 0;
        var failed = failedMatch.Success ? int.Parse(failedMatch.Groups[1].Value) : 0;
        var total = totalMatch.Success ? int.Parse(totalMatch.Groups[1].Value) : (passed + failed);

        // If no explicit counts found, check for success indicators
        if (total == 0)
        {
            if (output.Contains("✅") || output.Contains("Unity compatibility tests completed"))
            {
                passed = 1;
                total = 1;
            }
            else if (output.Contains("❌") || output.Contains("failed"))
            {
                failed = 1;
                total = 1;
            }
        }

        return (passed, failed, total);
    }

    private async Task<ProcessRunResult> RunProcessAsync(ProcessStartInfo processInfo, CancellationToken cancellationToken)
    {
        using var process = Process.Start(processInfo);
        if (process == null)
        {
            return new ProcessRunResult(-1, string.Empty, "Failed to start process");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw;
        }

        return new ProcessRunResult(process.ExitCode, await outputTask, await errorTask);
    }

    private record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);
}
