using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Testing;
using Nexo.Infrastructure.Testing.ExecutionPlatform;
using System.Text.RegularExpressions;

namespace Nexo.Tests.Infrastructure.Tests.MultiPlatform;

/// <summary>
/// Base class for multi-platform infrastructure tests.
/// 
/// Provides common functionality for executing platform-specific tests.
/// Uses hybrid approach:
/// - Execution platform (Docker, Rancher, Kubernetes, etc.) for platforms that support it
/// - Native execution for platforms that don't support containers (iOS)
/// 
/// All operations avoid command-line dependencies for maximum portability.
/// Execution platform is abstracted to allow users to plug in different orchestration systems.
/// </summary>
public abstract class MultiPlatformTestBase : TestBase
{
    protected readonly string _projectRoot;
    protected readonly string _platformName;
    protected readonly string _dotnetVersion;
    protected readonly string _description;
    protected readonly IExecutionPlatform? _executionPlatform;
    protected readonly ILogger<MultiPlatformTestBase>? _logger;
    protected readonly ExecutionMode _executionMode;

    protected MultiPlatformTestBase(
        string platformName, 
        string dotnetVersion, 
        string description,
        IExecutionPlatform? executionPlatform = null,
        ILogger<MultiPlatformTestBase>? logger = null)
    {
        _platformName = platformName;
        _dotnetVersion = dotnetVersion;
        _description = description;
        _projectRoot = DiscoverProjectRoot();
        _logger = logger;
        _executionMode = PlatformCapabilityDetector.GetExecutionMode(platformName);
        
        // Create execution platform only if platform supports containerization
        if (_executionMode == ExecutionMode.Docker)
        {
            _executionPlatform = executionPlatform ?? CreateDefaultExecutionPlatform();
        }
    }

    protected string DiscoverProjectRoot()
    {
        var current = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(current);
        
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "Nexo.CLI", "Nexo.CLI.csproj")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        
        return current;
    }

    /// <summary>
    /// Executes tests using the appropriate method (container platform or native) based on platform capabilities.
    /// </summary>
    protected async Task<TestResult> ExecuteTestAsync(
        string? dockerfile = null,
        Func<CancellationToken, Task<TestResult>>? nativeExecutor = null,
        CancellationToken cancellationToken = default)
    {
        // Use execution platform (Docker, Rancher, Kubernetes, etc.) if platform supports it
        if (_executionMode == ExecutionMode.Docker && dockerfile != null && _executionPlatform != null)
        {
            return await ExecuteContainerTestAsync(dockerfile, cancellationToken);
        }

        // Use native execution if required or if execution platform is not available
        if (_executionMode == ExecutionMode.Native && nativeExecutor != null)
        {
            return await nativeExecutor(cancellationToken);
        }

        // Fallback: try execution platform if available, otherwise native
        if (dockerfile != null && _executionPlatform != null)
        {
            var platformAvailable = await _executionPlatform.IsAvailableAsync(cancellationToken);
            if (platformAvailable)
            {
                return await ExecuteContainerTestAsync(dockerfile, cancellationToken);
            }
        }

        if (nativeExecutor != null)
        {
            return await nativeExecutor(cancellationToken);
        }

        // No execution method available
        return new TestResult
        {
            TestName = TestName,
            Category = Category,
            Passed = false,
            Message = $"No execution method available for {_platformName}",
            Duration = TimeSpan.Zero,
            ErrorMessage = $"Platform {_platformName} requires either a container execution platform or native execution, but neither is configured."
        };
    }

    protected async Task<TestResult> ExecuteContainerTestAsync(string dockerfile, CancellationToken cancellationToken)
    {
        if (_executionPlatform == null)
        {
            throw new InvalidOperationException($"Execution platform is not available for {_platformName}");
        }

        var startTime = DateTime.UtcNow;
        var dockerfilePath = Path.Combine(_projectRoot, dockerfile);

        if (!File.Exists(dockerfilePath))
        {
            return new TestResult
            {
                TestName = TestName,
                Category = Category,
                Passed = false,
                Message = $"Dockerfile not found: {dockerfilePath}",
                Duration = DateTime.UtcNow - startTime
            };
        }

        // Check execution platform availability
        if (!await _executionPlatform.IsAvailableAsync(cancellationToken))
        {
            return new TestResult
            {
                TestName = TestName,
                Category = Category,
                Passed = false,
                Message = $"{_executionPlatform.PlatformName} is not available for {_platformName}",
                Duration = DateTime.UtcNow - startTime
            };
        }

        try
        {
            var imageTag = $"nexo-framework-test:{_platformName}";
            var resultsDir = Path.Combine(_projectRoot, "test-results", "framework");
            Directory.CreateDirectory(resultsDir);

            // Build container image using execution platform
            _logger?.LogInformation("Building {Platform} image: {ImageTag}", _executionPlatform.PlatformName, imageTag);
            var buildArgs = new Dictionary<string, string> { ["DOTNET_VERSION"] = _dotnetVersion };
            var buildResult = await _executionPlatform.BuildImageAsync(
                dockerfilePath,
                imageTag,
                _projectRoot,
                buildArgs,
                null,
                cancellationToken);

            if (!buildResult.Success)
            {
                return new TestResult
                {
                    TestName = TestName,
                    Category = Category,
                    Passed = false,
                    Message = $"{_executionPlatform.PlatformName} build failed for {_platformName}",
                    Duration = buildResult.Duration,
                    ErrorMessage = buildResult.ErrorMessage
                };
            }

            // Prepare test command (embedded in C# - no shell scripts)
            var isWindows = dockerfile.Contains("windows", StringComparison.OrdinalIgnoreCase);
            var testCommand = BuildTestCommand(isWindows);

            // Run tests in container using execution platform
            _logger?.LogInformation("Running tests in {Platform} container: {ImageTag}", _executionPlatform.PlatformName, imageTag);
            var envVars = new Dictionary<string, string> { ["DOTNET_VERSION"] = _dotnetVersion };
            var volumeMounts = new Dictionary<string, string> { [resultsDir] = "/workspace/test-results" };

            var runResult = await _executionPlatform.RunContainerAsync(
                imageTag,
                testCommand,
                envVars,
                volumeMounts,
                null,
                cancellationToken);

            var duration = DateTime.UtcNow - startTime;

            // Parse results from container output
            var output = runResult.StandardOutput + runResult.StandardError;
            var (passed, failed, total) = ParseTestResults(output);

            return new TestResult
            {
                TestName = TestName,
                Category = Category,
                Passed = runResult.Success && (total == 0 || failed == 0),
                Message = $"{_description}: {total} total, {passed} passed, {failed} failed",
                Duration = duration,
                ErrorMessage = runResult.Success ? null : runResult.StandardError,
                Metadata = new Dictionary<string, object>
                {
                    ["Platform"] = _platformName,
                    ["DotNetVersion"] = _dotnetVersion,
                    ["ExecutionPlatform"] = _executionPlatform.PlatformName,
                    ["ExecutionMode"] = "Container",
                    ["TotalTests"] = total,
                    ["PassedTests"] = passed,
                    ["FailedTests"] = failed,
                    ["ContainerId"] = runResult.ContainerId ?? string.Empty
                }
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = TestName,
                Category = Category,
                Passed = false,
                Message = $"Error running {_platformName} tests: {ex.Message}",
                Duration = DateTime.UtcNow - startTime,
                ErrorMessage = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Builds the test command array for container execution.
    /// Embedded in C# - no shell scripts required.
    /// </summary>
    protected virtual string[] BuildTestCommand(bool isWindows)
    {
        var testFilter = "FullyQualifiedName~BaseFrameworkSmokeTests";
        var resultsFile = isWindows 
            ? $"{_platformName}-base-framework.trx"
            : $"{_platformName}-base-framework.trx";
        var resultsDir = isWindows ? "C:\\workspace\\test-results" : "/workspace/test-results";
        var testProject = isWindows
            ? "src\\Nexo.Tests.Infrastructure\\Nexo.Tests.Infrastructure.csproj"
            : "src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj";

        if (isWindows)
        {
            return new[]
            {
                "cmd", "/c",
                $"cd C:\\workspace && dotnet test \"{testProject}\" --filter \"{testFilter}\" --logger \"console;verbosity=minimal\" --logger \"trx;LogFileName={resultsFile}\" --results-directory \"{resultsDir}\""
            };
        }
        else
        {
            return new[]
            {
                "bash", "-c",
                $"cd /workspace && dotnet test \"{testProject}\" --filter \"{testFilter}\" --logger \"console;verbosity=minimal\" --logger \"trx;LogFileName={resultsFile}\" --results-directory \"{resultsDir}\""
            };
        }
    }

    protected (int passed, int failed, int total) ParseTestResults(string output)
    {
        // Try to extract test counts from output
        var passedMatch = Regex.Match(output, @"(\d+)\s+Passed!", RegexOptions.IgnoreCase);
        var failedMatch = Regex.Match(output, @"(\d+)\s+Failed!", RegexOptions.IgnoreCase);
        var totalMatch = Regex.Match(output, @"(\d+)\s+Total", RegexOptions.IgnoreCase);

        var passed = passedMatch.Success ? int.Parse(passedMatch.Groups[1].Value) : 0;
        var failed = failedMatch.Success ? int.Parse(failedMatch.Groups[1].Value) : 0;
        var total = totalMatch.Success ? int.Parse(totalMatch.Groups[1].Value) : (passed + failed);

        // If no explicit counts found, check for success indicators
        if (total == 0)
        {
            if (output.Contains("✅") || output.Contains("passed", StringComparison.OrdinalIgnoreCase))
            {
                passed = 1;
                total = 1;
            }
            else if (output.Contains("❌") || output.Contains("failed", StringComparison.OrdinalIgnoreCase))
            {
                failed = 1;
                total = 1;
            }
        }

        return (passed, failed, total);
    }

    /// <summary>
    /// Creates the default execution platform (Docker).
    /// Users can override this or inject a custom IExecutionPlatform via constructor.
    /// </summary>
    private static IExecutionPlatform CreateDefaultExecutionPlatform()
    {
        // Create a minimal logger factory for execution platform
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<DockerExecutionPlatform>();
        return new DockerExecutionPlatform(logger);
    }
}
