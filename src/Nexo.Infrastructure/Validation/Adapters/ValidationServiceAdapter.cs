using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Core.Application.Common.Models;
using Nexo.Infrastructure.Validation.Parsers;
using Nexo.Tools.Dev;
using Nexo.Abstractions;
using System.Text.Json;
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

            // Use DotnetTestTool to run tests
            var testTool = new DotnetTestTool();
            var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

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

                    var testCall = new ToolCall(
                        "dotnet.test",
                        JsonDocument.Parse($$"""{"root":"{{projectDir}}"}""").RootElement);

                    var result = await testTool.InvokeAsync(testCall, snapshot, cancellationToken);

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
                        // Fallback to simple result parsing
                        if (result.Payload is System.Text.Json.JsonElement jsonElement)
                        {
                            var ok = jsonElement.TryGetProperty("ok", out var okElement) && okElement.GetBoolean();
                            
                            allTestResults.Add(new TestResult
                            {
                                Name = testProject.Name,
                                Passed = ok,
                                Message = ok ? "Tests passed" : "Test execution failed"
                            });
                            
                            if (ok) totalTestsPassed++;
                            else totalTestsFailed++;
                            totalTestsRun++;
                        }
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
}
