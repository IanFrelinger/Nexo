using System.Reflection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Testing.Ports;

namespace Nexo.Infrastructure.Testing;

/// <summary>
/// Infrastructure adapter for test discovery and execution.
/// 
/// Implements ITestRunner port from Application layer. Provides:
/// - Test discovery via reflection (scans assemblies for TestBase subclasses)
/// - Test filtering by name or category
/// - Test execution with setup/cleanup lifecycle
/// - Progress tracking for test execution
/// - Result aggregation and reporting
/// 
/// Part of the Infrastructure layer, implementing the hexagonal architecture pattern.
/// </summary>
public class TestRunnerAdapter : ITestRunner
{
    private static readonly TimeSpan DefaultPerTestTimeout = TimeSpan.FromSeconds(60);

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
            Categories = results.Select(r => r.Category).Where(c => c != null).Distinct().Select(c => c!).ToList()
        };
    }

    private List<TestBase> DiscoverTests(string? filter)
    {
        var tests = new List<TestBase>();
        
        // Get already loaded assemblies
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .ToList();
        
        // Also try to load test assemblies from known locations
        var baseDir = AppContext.BaseDirectory;
        var testAssemblyNames = new[]
        {
            "Nexo.Tests.Domain",
            "Nexo.Tests.Application",
            "Nexo.Tests.Infrastructure",
            "Nexo.Tests.CLI",
            "Nexo.Tests.GeoTerrain",
            "Nexo.Tests.GeoVector",
            "Nexo.Tests.GeoWorld",
            "Nexo.Tests.Orchestration"
        };
        
        var assemblies = new List<System.Reflection.Assembly>(loadedAssemblies);
        
        // Try loading by name first (works if already referenced)
        foreach (var assemblyName in testAssemblyNames)
        {
            try
            {
                var assembly = System.Reflection.Assembly.Load(assemblyName);
                if (!assemblies.Any(a => a.FullName == assembly.FullName))
                {
                    assemblies.Add(assembly);
                    _logger.LogDebug("Loaded test assembly by name: {Name}", assemblyName);
                }
            }
            catch
            {
                // Try loading from file - check multiple possible locations
                var possiblePaths = new[]
                {
                    Path.Combine(baseDir, $"{assemblyName}.dll"),
                    Path.Combine(baseDir, "..", "..", "..", "src", assemblyName, "bin", "Debug", "net8.0", $"{assemblyName}.dll"),
                    Path.Combine(baseDir, "..", "..", "..", "src", assemblyName, "bin", "Release", "net8.0", $"{assemblyName}.dll"),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", assemblyName, "bin", "Debug", "net8.0", $"{assemblyName}.dll")
                    ,
                    Path.Combine(Directory.GetCurrentDirectory(), "src", assemblyName, "bin", "Release", "net8.0", $"{assemblyName}.dll")
                };
                
                foreach (var assemblyPath in possiblePaths)
                {
                    if (File.Exists(assemblyPath))
                    {
                        try
                        {
                            var assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
                            if (!assemblies.Any(a => a.FullName == assembly.FullName))
                            {
                                assemblies.Add(assembly);
                                _logger.LogDebug("Loaded test assembly from file: {Path}", assemblyPath);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Failed to load test assembly: {Path}", assemblyPath);
                        }
                    }
                }
            }
        }

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

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(DefaultPerTestTimeout);
        var ct = timeoutCts.Token;

        var runTask = RunTestCoreAsync(test, ct, startTime, testName, category);
        var timeoutTask = Task.Delay(DefaultPerTestTimeout, ct);
        var completed = await Task.WhenAny(runTask, timeoutTask);

        if (completed == timeoutTask)
        {
            try
            {
                await test.CleanupAsync(CancellationToken.None);
            }
            catch
            {
                // Ignore cleanup errors on timeout
            }
            return new TestResult
            {
                Name = testName,
                Category = category,
                Passed = false,
                Duration = DateTime.UtcNow - startTime,
                ErrorMessage = $"Test timed out after {DefaultPerTestTimeout.TotalSeconds}s"
            };
        }
        return await runTask;
    }

    private async Task<TestResult> RunTestCoreAsync(
        TestBase test,
        CancellationToken cancellationToken,
        DateTimeOffset startTime,
        string testName,
        string category)
    {
        try
        {
            await test.SetupAsync(cancellationToken);

            var result = await test.ExecuteAsync(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            await test.CleanupAsync(cancellationToken);

            return result with
            {
                Name = testName,
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
                Name = testName,
                Category = category,
                Passed = false,
                Duration = duration,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }
}

