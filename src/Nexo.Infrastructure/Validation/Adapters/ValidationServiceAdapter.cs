using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Tools.Dev;
using Nexo.Abstractions;
using System.Text.Json;
using System.Diagnostics;

namespace Nexo.Infrastructure.Validation.Adapters;

/// <summary>
/// Infrastructure adapter for running validation tests.
/// Implements IValidationService port from Application layer.
/// </summary>
public class ValidationServiceAdapter : IValidationService
{
    private readonly ILogger<ValidationServiceAdapter> _logger;

    public ValidationServiceAdapter(ILogger<ValidationServiceAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationResult> ValidateAsync(
        string? filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Running validation with filter: {Filter}",
            filter ?? "none");

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
                return new ValidationResult
                {
                    Passed = true,
                    Message = "No test projects found - validation skipped",
                    TestsRun = 0,
                    TestsPassed = 0,
                    TestsFailed = 0
                };
            }

            // Use DotnetTestTool to run tests
            var testTool = new DotnetTestTool();
            var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

            var testResults = new List<TestResult>();
            int totalTestsRun = 0;
            int totalTestsPassed = 0;
            int totalTestsFailed = 0;

            foreach (var testProject in testProjects)
            {
                try
                {
                    var projectDir = testProject.Directory?.FullName ?? currentDir.FullName;

                    var testCall = new ToolCall(
                        "dotnet.test",
                        JsonDocument.Parse($$"""{"root":"{{projectDir}}"}""").RootElement);

                    var result = await testTool.InvokeAsync(testCall, snapshot, cancellationToken);

                    // Parse result
                    if (result.Payload is System.Text.Json.JsonElement jsonElement)
                    {
                        var ok = jsonElement.TryGetProperty("ok", out var okElement) && okElement.GetBoolean();
                        
                        if (ok)
                        {
                            totalTestsPassed++;
                            testResults.Add(new TestResult
                            {
                                Name = testProject.Name,
                                Passed = true,
                                Message = "Tests passed"
                            });
                        }
                        else
                        {
                            totalTestsFailed++;
                            var stderr = jsonElement.TryGetProperty("stderr", out var stderrElement)
                                ? stderrElement.GetString()
                                : "Test execution failed";
                            
                            testResults.Add(new TestResult
                            {
                                Name = testProject.Name,
                                Passed = false,
                                Message = stderr
                            });
                        }
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
                    testResults.Add(new TestResult
                    {
                        Name = testProject.Name,
                        Passed = false,
                        Message = $"Error: {ex.Message}"
                    });
                }
            }

            // Pass if no tests failed (even if no tests were run)
            var passed = totalTestsFailed == 0;

            return new ValidationResult
            {
                Passed = passed,
                Message = passed
                    ? $"Validation passed ({totalTestsPassed}/{totalTestsRun} tests)"
                    : $"Validation failed ({totalTestsFailed}/{totalTestsRun} tests failed)",
                TestsRun = totalTestsRun,
                TestsPassed = totalTestsPassed,
                TestsFailed = totalTestsFailed,
                TestResults = testResults
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
