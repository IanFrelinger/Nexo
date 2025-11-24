using System.Reflection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Testing.Ports;

namespace Nexo.Infrastructure.Testing;

/// <summary>
/// Infrastructure adapter for test discovery and execution.
/// </summary>
public class TestRunnerAdapter : ITestRunner
{
    private readonly ILogger<TestRunnerAdapter> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TestRunnerAdapter(
        ILogger<TestRunnerAdapter> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<TestExecutionResult> RunTestsAsync(
        string? filter = null,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering tests with filter: {Filter}", filter ?? "none");

        progress?.Report(new ProgressReport
        {
            Percentage = 0,
            Message = "Discovering tests...",
            CurrentStep = 0,
            TotalSteps = null
        });

        var tests = DiscoverTests(filter);
        var totalTests = tests.Count;

        _logger.LogInformation("Found {Count} test(s) to execute", totalTests);

        progress?.Report(new ProgressReport
        {
            Percentage = 5,
            Message = $"Found {totalTests} test(s) to execute",
            CurrentStep = 0,
            TotalSteps = totalTests
        });

        var results = new List<TestResult>();
        var startTime = DateTime.UtcNow;
        var currentTest = 0;

        foreach (var test in tests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            currentTest++;
            var percentage = 5 + (int)((currentTest / (double)totalTests) * 90);

            progress?.Report(new ProgressReport
            {
                Percentage = percentage,
                Message = $"Running {test.TestName} ({currentTest}/{totalTests})",
                CurrentStep = currentTest,
                TotalSteps = totalTests,
                Metadata = new Dictionary<string, object>
                {
                    ["TestName"] = test.TestName,
                    ["Category"] = test.Category
                }
            });

            var testResult = await ExecuteTestAsync(test, cancellationToken);
            results.Add(testResult);
        }

        var duration = DateTime.UtcNow - startTime;
        var passedTests = results.Count(r => r.Passed);
        var failedTests = results.Count(r => !r.Passed);

        progress?.Report(new ProgressReport
        {
            Percentage = 100,
            Message = $"Tests completed: {passedTests}/{totalTests} passed",
            CurrentStep = totalTests,
            TotalSteps = totalTests
        });

        return new TestExecutionResult
        {
            TotalTests = totalTests,
            PassedTests = passedTests,
            FailedTests = failedTests,
            TotalDuration = duration,
            Results = results,
            Categories = results.Select(r => r.Category).Distinct().ToList()
        };
    }

    private List<TestBase> DiscoverTests(string? filter)
    {
        var tests = new List<TestBase>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .ToList();

        foreach (var assembly in assemblies)
        {
            try
            {
                var testTypes = assembly.GetTypes()
                    .Where(t => typeof(TestBase).IsAssignableFrom(t) &&
                               !t.IsAbstract &&
                               !t.IsInterface)
                    .ToList();

                foreach (var testType in testTypes)
                {
                    if (!string.IsNullOrEmpty(filter))
                    {
                        var testName = testType.Name;
                        var category = testType.Namespace?.Split('.').LastOrDefault() ?? "Unknown";
                        
                        if (!testName.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                            !category.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    try
                    {
                        var test = CreateTestInstance(testType);
                        if (test != null)
                        {
                            tests.Add(test);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create test instance: {TestType}", testType.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan assembly: {Assembly}", assembly.FullName);
            }
        }

        return tests;
    }

    private TestBase? CreateTestInstance(Type testType)
    {
        // Try constructor injection first
        var constructors = testType.GetConstructors();
        foreach (var constructor in constructors)
        {
            try
            {
                var parameters = constructor.GetParameters();
                var args = new object[parameters.Length];
                var canResolve = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    try
                    {
                        args[i] = _serviceProvider.GetService(paramType) ??
                                 Activator.CreateInstance(paramType) ??
                                 throw new InvalidOperationException($"Cannot resolve {paramType.Name}");
                    }
                    catch
                    {
                        canResolve = false;
                        break;
                    }
                }

                if (canResolve)
                {
                    return (TestBase)Activator.CreateInstance(testType, args)!;
                }
            }
            catch
            {
                // Try next constructor
            }
        }

        // Fallback to parameterless constructor
        try
        {
            return (TestBase)Activator.CreateInstance(testType)!;
        }
        catch
        {
            return null;
        }
    }

    private async Task<TestResult> ExecuteTestAsync(TestBase test, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var testName = test.TestName;
        var category = test.Category;

        try
        {
            await test.SetupAsync(cancellationToken);

            var result = await test.ExecuteAsync(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            await test.CleanupAsync(cancellationToken);

            return result with
            {
                TestName = testName,
                Category = category,
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            await test.CleanupAsync(cancellationToken);

            return new TestResult
            {
                TestName = testName,
                Category = category,
                Passed = false,
                Duration = duration,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }
}

