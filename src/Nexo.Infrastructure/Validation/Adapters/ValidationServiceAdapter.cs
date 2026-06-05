using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Core.Application.Common.Models;
using Nexo.Infrastructure.Validation.Parsers;
using System.Diagnostics;

namespace Nexo.Infrastructure.Validation.Adapters;

/// <summary>
/// Infrastructure adapter for running validation tests.
/// 
/// Implements IValidationService port from Application layer. Provides:
/// - Test project discovery
/// - Test execution via dotnet test
/// - Test result parsing using ITestResultParser
/// - Progress tracking for test execution
/// 
/// Part of the Infrastructure layer, implementing the hexagonal architecture pattern.
/// </summary>
public class ValidationServiceAdapter : IValidationService
{
    private readonly ILogger<ValidationServiceAdapter> _logger;
    private readonly ITestResultParser _testResultParser;

    public ValidationServiceAdapter(
        ILogger<ValidationServiceAdapter> logger,
        ITestResultParser testResultParser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _testResultParser = testResultParser ?? throw new ArgumentNullException(nameof(testResultParser));
    }

    public async Task<ValidationResult> ValidateAsync(
        string? filter,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Running validation with filter: {Filter}",
            filter ?? "none");

        progress?.Report(new ProgressReport
        {
            Percentage = 0,
            Message = $"Starting validation with filter: {filter ?? "none"}",
            CurrentStep = 0,
            TotalSteps = null
        });

        try
        {
            // Find test projects in current directory
            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            var testProjects = currentDir.GetFiles("*.csproj", SearchOption.AllDirectories)
                .Where(f => f.Name.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
                           f.DirectoryName?.Contains("test", StringComparison.OrdinalIgnoreCase) == true)
                .Where(f => !f.Name.Equals("copy-assemblies.csproj", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (testProjects.Count == 0)
            {
                _logger.LogInformation("No test projects found - validation skipped");
                progress?.Report(new ProgressReport
                {
                    Percentage = 100,
                    Message = "No test projects found - validation skipped"
                });
                return new ValidationResult
                {
                    Passed = true,
                    Message = "No test projects found - validation skipped",
                    TestsRun = 0,
                    TestsPassed = 0,
                    TestsFailed = 0
                };
            }

            progress?.Report(new ProgressReport
            {
                Percentage = 5,
                Message = $"Found {testProjects.Count} test project(s)",
                CurrentStep = 0,
                TotalSteps = testProjects.Count
            });

            var allTestResults = new List<TestResult>();
            int totalTestsRun = 0;
            int totalTestsPassed = 0;
            int totalTestsFailed = 0;
            var totalProjects = testProjects.Count;
            var currentProject = 0;

            foreach (var testProject in testProjects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                currentProject++;
                var percentage = 5 + (int)((currentProject / (double)totalProjects) * 90);

                progress?.Report(new ProgressReport
                {
                    Percentage = percentage,
                    Message = $"Running tests in {Path.GetFileName(testProject.FullName)} ({currentProject}/{totalProjects})",
                    CurrentStep = currentProject,
                    TotalSteps = totalProjects,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Project"] = testProject.FullName
                    }
                });

                try
                {
                    var projectDir = testProject.Directory?.FullName ?? currentDir.FullName;
                    var csprojPath = testProject.FullName;

                    // `dotnet test --no-build` requires outputs on disk. Multi-target test projects
                    // often have no DLL until an explicit build; CI `nexo validate` previously failed
                    // with "test source file ... was not found" when only the CLI had been built.
                    var buildExit = await RunDotnetBuildProjectAsync(csprojPath, cancellationToken).ConfigureAwait(false);
                    if (buildExit != 0)
                    {
                        _logger.LogWarning(
                            "Skipping tests for {Project}: dotnet build exited {ExitCode}",
                            testProject.Name,
                            buildExit);
                        totalTestsFailed++;
                        totalTestsRun++;
                        allTestResults.Add(new TestResult
                        {
                            Name = testProject.Name,
                            Passed = false,
                            Message = $"dotnet build failed (exit {buildExit})"
                        });
                        continue;
                    }

                    // Single net8.0 run avoids duplicate net8+net9 hosts under load (validate is not a full matrix run).
                    var exitCode = await RunDotnetTestForValidateAsync(
                        csprojPath,
                        streamOutput: progress != null,
                        cancellationToken).ConfigureAwait(false);

                    // Try to find and parse TRX files
                    var trxFiles = Directory.GetFiles(
                        projectDir,
                        "*.trx",
                        SearchOption.AllDirectories)
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .Take(1) // Get most recent TRX file
                        .Select(f => new FileInfo(f))
                        .ToList();

                    if (trxFiles.Any())
                    {
                        // Parse TRX file for detailed results
                        var parsedResults = await _testResultParser.ParseAsync(trxFiles.First(), cancellationToken);
                        allTestResults.AddRange(parsedResults);
                        
                        totalTestsRun += parsedResults.Count;
                        totalTestsPassed += parsedResults.Count(r => r.Passed);
                        totalTestsFailed += parsedResults.Count(r => !r.Passed);
                    }
                    else
                    {
                        // Fallback when no TRX: use exit code
                        allTestResults.Add(new TestResult
                        {
                            Name = testProject.Name,
                            Passed = exitCode == 0,
                            Message = exitCode == 0 ? "Tests passed" : "Test execution failed"
                        });
                        if (exitCode == 0) totalTestsPassed++;
                        else totalTestsFailed++;
                        totalTestsRun++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to run tests for project: {Project}",
                        testProject.Name);

                    totalTestsFailed++;
                    allTestResults.Add(new TestResult
                    {
                        Name = testProject.Name,
                        Passed = false,
                        Message = $"Error: {ex.Message}"
                    });
                }
            }

            // Pass if no tests failed (even if no tests were run)
            var passed = totalTestsFailed == 0;

            progress?.Report(new ProgressReport
            {
                Percentage = 100,
                Message = $"Validation completed. Passed: {passed}, Tests: {totalTestsPassed}/{totalTestsRun}",
                CurrentStep = totalProjects,
                TotalSteps = totalProjects
            });

            return new ValidationResult
            {
                Passed = passed,
                Message = passed
                    ? $"Validation passed ({totalTestsPassed}/{totalTestsRun} tests)"
                    : $"Validation failed ({totalTestsFailed}/{totalTestsRun} tests failed)",
                TestsRun = totalTestsRun,
                TestsPassed = totalTestsPassed,
                TestsFailed = totalTestsFailed,
                TestResults = allTestResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation");
            return new ValidationResult
            {
                Passed = false,
                Message = $"Validation error: {ex.Message}",
                TestsRun = 0,
                TestsPassed = 0,
                TestsFailed = 0
            };
        }
    }

    private static async Task<int> RunDotnetBuildProjectAsync(string csprojPath, CancellationToken ct)
    {
        var args = $"build \"{csprojPath}\" --verbosity quiet";
        var psi = new ProcessStartInfo("dotnet", args)
        {
            WorkingDirectory = Path.GetDirectoryName(csprojPath) ?? Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var p = Process.Start(psi);
        if (p is null)
            return -1;

        using (p)
        {
            p.OutputDataReceived += (_, _) => { };
            p.ErrorDataReceived += (_, e) => { if (e.Data is not null) Console.Error.WriteLine(e.Data); };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            try
            {
                await p.WaitForExitAsync(ct).ConfigureAwait(false);
                return p.ExitCode;
            }
            catch (OperationCanceledException)
            {
                try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return -1;
            }
        }
    }

    /// <summary>
    /// Runs <c>dotnet test</c> for validate: net8.0 only, TRX for parsing, optional console streaming.
    /// </summary>
    private static async Task<int> RunDotnetTestForValidateAsync(string csprojPath, bool streamOutput, CancellationToken ct)
    {
        var verbosity = streamOutput ? "normal" : "minimal";
        var args =
            $"test \"{csprojPath}\" --framework net8.0 --no-build " +
            "--filter \"Category!=DockerOptional&Category!=Stress&FullyQualifiedName!~BootstrapRuntimeAssessTests\" " +
            "--logger trx --blame-hang-timeout 120s --blame-hang-dump-type none " +
            $"--verbosity {verbosity}";
        var workDir = Path.GetDirectoryName(csprojPath) ?? Directory.GetCurrentDirectory();
        var psi = new ProcessStartInfo("dotnet", args)
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var p = Process.Start(psi);
        if (p is null)
            return -1;

        using (p)
        {
            if (streamOutput)
            {
                p.OutputDataReceived += (_, e) => { if (e.Data is not null) Console.Out.WriteLine(e.Data); };
                p.ErrorDataReceived += (_, e) => { if (e.Data is not null) Console.Error.WriteLine(e.Data); };
            }
            else
            {
                p.OutputDataReceived += (_, _) => { };
                p.ErrorDataReceived += (_, e) => { if (e.Data is not null) Console.Error.WriteLine(e.Data); };
            }

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            try
            {
                await p.WaitForExitAsync(ct).ConfigureAwait(false);
                return p.ExitCode;
            }
            catch (OperationCanceledException)
            {
                try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return -1;
            }
        }
    }
}
