using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Infrastructure.Testing;

/// <summary>
/// Unity-specific test runner that executes framework tests in Unity environment.
/// 
/// Provides:
/// - Unity Editor detection and execution
/// - .NET Standard 2.0 compatibility testing
/// - Unity Test Runner integration
/// - Framework test execution (Base Framework + Stress Tests)
/// 
/// Part of the Infrastructure layer, implementing Unity test execution.
/// </summary>
public class UnityTestRunner
{
    private readonly ILogger<UnityTestRunner> _logger;
    private readonly string _projectRoot;

    public UnityTestRunner(ILogger<UnityTestRunner> logger, string projectRoot)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _projectRoot = projectRoot ?? throw new ArgumentNullException(nameof(projectRoot));
    }

    /// <summary>
    /// Runs framework tests in Unity environment.
    /// </summary>
    public async Task<TestExecutionResult> RunFrameworkTestsAsync(
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running framework tests in Unity environment");

        progress?.Report(new ProgressReport
        {
            Percentage = 0,
            Message = "Discovering Unity environment...",
            CurrentStep = 0,
            TotalSteps = 4
        });

        var results = new List<TestResult>();
        var startTime = DateTime.UtcNow;

        // Phase 1: Test .NET Standard 2.0 compatibility
        progress?.Report(new ProgressReport
        {
            Percentage = 25,
            Message = "Testing .NET Standard 2.0 compatibility...",
            CurrentStep = 1,
            TotalSteps = 4
        });

        var domainTestResult = await RunDomainTestsAsync(cancellationToken);
        results.Add(domainTestResult);

        // Phase 2: Test Core.Domain build
        progress?.Report(new ProgressReport
        {
            Percentage = 50,
            Message = "Building Core.Domain for Unity...",
            CurrentStep = 2,
            TotalSteps = 4
        });

        var buildResult = await BuildCoreDomainAsync(cancellationToken);
        results.Add(buildResult);

        // Phase 3: Test Unity UI components
        progress?.Report(new ProgressReport
        {
            Percentage = 75,
            Message = "Building Unity UI components...",
            CurrentStep = 3,
            TotalSteps = 4
        });

        var uiBuildResult = await BuildUnityUIAsync(cancellationToken);
        results.Add(uiBuildResult);

        // Phase 4: Run Unity Test Runner (if available)
        progress?.Report(new ProgressReport
        {
            Percentage = 90,
            Message = "Running Unity Test Runner...",
            CurrentStep = 4,
            TotalSteps = 4
        });

        var unityTestResult = await RunUnityTestRunnerAsync(cancellationToken);
        if (unityTestResult != null)
        {
            results.Add(unityTestResult);
        }

        var totalDuration = DateTime.UtcNow - startTime;
        var passed = results.Count(r => r.Passed);
        var failed = results.Count(r => !r.Passed);

        progress?.Report(new ProgressReport
        {
            Percentage = 100,
            Message = $"Unity tests completed: {passed} passed, {failed} failed",
            CurrentStep = 4,
            TotalSteps = 4
        });

        return new TestExecutionResult
        {
            TotalTests = results.Count,
            PassedTests = passed,
            FailedTests = failed,
            TotalDuration = totalDuration,
            Results = results
        };
    }

    private async Task<TestResult> RunDomainTestsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var testProject = Path.Combine(_projectRoot, "src", "Nexo.Tests.Domain", "Nexo.Tests.Domain.csproj");

            if (!File.Exists(testProject))
            {
                return new TestResult
                {
                    TestName = "DomainTests_UnityCompatibility",
                    Category = "Unity",
                    Passed = false,
                    Message = "Domain test project not found",
                    Duration = TimeSpan.Zero
                };
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"test \"{testProject}\" --filter \"FullyQualifiedName~Domain\" --framework netstandard2.0 --logger \"console;verbosity=minimal\"",
                WorkingDirectory = _projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var result = await RunProcessAsync(processInfo, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            var passed = result.ExitCode == 0;
            var output = result.StandardOutput + result.StandardError;

            return new TestResult
            {
                TestName = "DomainTests_UnityCompatibility",
                Category = "Unity",
                Passed = passed,
                Message = passed ? "Domain tests passed (Unity-compatible)" : "Domain tests failed",
                Duration = duration,
                ErrorMessage = passed ? null : output
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = "DomainTests_UnityCompatibility",
                Category = "Unity",
                Passed = false,
                Message = $"Error running domain tests: {ex.Message}",
                Duration = TimeSpan.Zero,
                ErrorMessage = ex.ToString()
            };
        }
    }

    private async Task<TestResult> BuildCoreDomainAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var project = Path.Combine(_projectRoot, "src", "Nexo.Core.Domain", "Nexo.Core.Domain.csproj");

            if (!File.Exists(project))
            {
                return new TestResult
                {
                    TestName = "CoreDomain_Build",
                    Category = "Unity",
                    Passed = false,
                    Message = "Core.Domain project not found",
                    Duration = TimeSpan.Zero
                };
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{project}\" --framework netstandard2.0 --no-incremental",
                WorkingDirectory = _projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var result = await RunProcessAsync(processInfo, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            var passed = result.ExitCode == 0;

            return new TestResult
            {
                TestName = "CoreDomain_Build",
                Category = "Unity",
                Passed = passed,
                Message = passed ? "Core.Domain builds for .NET Standard 2.0 (Unity compatible)" : "Core.Domain build failed",
                Duration = duration,
                ErrorMessage = passed ? null : result.StandardError
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = "CoreDomain_Build",
                Category = "Unity",
                Passed = false,
                Message = $"Error building Core.Domain: {ex.Message}",
                Duration = TimeSpan.Zero,
                ErrorMessage = ex.ToString()
            };
        }
    }

    private async Task<TestResult> BuildUnityUIAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var project = Path.Combine(_projectRoot, "src", "Nexo.Core.UI.Unity", "Nexo.Core.UI.Unity.csproj");

            if (!File.Exists(project))
            {
                return new TestResult
                {
                    TestName = "UnityUI_Build",
                    Category = "Unity",
                    Passed = false,
                    Message = "Unity UI project not found",
                    Duration = TimeSpan.Zero
                };
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{project}\" --no-incremental",
                WorkingDirectory = _projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var result = await RunProcessAsync(processInfo, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            var passed = result.ExitCode == 0;

            return new TestResult
            {
                TestName = "UnityUI_Build",
                Category = "Unity",
                Passed = passed,
                Message = passed ? "Unity UI components build successfully" : "Unity UI build failed",
                Duration = duration,
                ErrorMessage = passed ? null : result.StandardError
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = "UnityUI_Build",
                Category = "Unity",
                Passed = false,
                Message = $"Error building Unity UI: {ex.Message}",
                Duration = TimeSpan.Zero,
                ErrorMessage = ex.ToString()
            };
        }
    }

    private async Task<TestResult?> RunUnityTestRunnerAsync(CancellationToken cancellationToken)
    {
        var unityExe = FindUnityExecutable();
        if (unityExe == null)
        {
            _logger.LogInformation("Unity Editor not found, skipping Unity Test Runner");
            return null;
        }

        var unityProject = FindUnityProject();
        if (unityProject == null)
        {
            _logger.LogInformation("Unity project not found, skipping Unity Test Runner");
            return null;
        }

        try
        {
            var startTime = DateTime.UtcNow;
            var resultsFile = Path.Combine(_projectRoot, "test-results", "unity-test-results.xml");
            var logsFile = Path.Combine(_projectRoot, "test-results", "unity-test.log");

            Directory.CreateDirectory(Path.GetDirectoryName(resultsFile)!);

            var processInfo = new ProcessStartInfo
            {
                FileName = unityExe,
                Arguments = $"-batchmode -nographics -projectPath \"{unityProject}\" " +
                           $"-runTests -testPlatform EditMode " +
                           $"-testResults \"{resultsFile}\" " +
                           $"-logFile \"{logsFile}\" " +
                           $"-quit",
                WorkingDirectory = _projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var result = await RunProcessAsync(processInfo, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            var passed = result.ExitCode == 0 && File.Exists(resultsFile);

            return new TestResult
            {
                TestName = "UnityTestRunner_Execution",
                Category = "Unity",
                Passed = passed,
                Message = passed ? "Unity Test Runner completed successfully" : "Unity Test Runner failed or produced no results",
                Duration = duration,
                ErrorMessage = passed ? null : result.StandardError
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = "UnityTestRunner_Execution",
                Category = "Unity",
                Passed = false,
                Message = $"Error running Unity Test Runner: {ex.Message}",
                Duration = TimeSpan.Zero,
                ErrorMessage = ex.ToString()
            };
        }
    }

    private string? FindUnityExecutable()
    {
        // Check environment variable first
        var envUnity = Environment.GetEnvironmentVariable("UNITY_BIN");
        if (!string.IsNullOrEmpty(envUnity) && File.Exists(envUnity))
        {
            return envUnity;
        }

        // Platform-specific discovery
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var hubPath = "/Applications/Unity/Hub/Editor";
            if (Directory.Exists(hubPath))
            {
                var versions = Directory.GetDirectories(hubPath)
                    .OrderByDescending(d => d)
                    .ToList();

                foreach (var versionDir in versions)
                {
                    var unityPath = Path.Combine(versionDir, "Unity.app", "Contents", "MacOS", "Unity");
                    if (File.Exists(unityPath))
                    {
                        return unityPath;
                    }
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var hubPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity", "Hub", "Editor");
            if (Directory.Exists(hubPath))
            {
                var versions = Directory.GetDirectories(hubPath)
                    .OrderByDescending(d => d)
                    .ToList();

                foreach (var versionDir in versions)
                {
                    var unityPath = Path.Combine(versionDir, "Editor", "Unity.exe");
                    if (File.Exists(unityPath))
                    {
                        return unityPath;
                    }
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var hubPath = "/opt/unity/Editor";
            var unityPath = Path.Combine(hubPath, "Unity");
            if (File.Exists(unityPath))
            {
                return unityPath;
            }
        }

        return null;
    }

    private string? FindUnityProject()
    {
        // Check common Unity project locations
        var candidates = new[]
        {
            Path.Combine(_projectRoot, "DirectorStudioUnity"),
            Path.Combine(_projectRoot, "unity-project"),
            Path.Combine(_projectRoot, "UnityTestProject")
        };

        foreach (var candidate in candidates)
        {
            var projectVersionFile = Path.Combine(candidate, "ProjectSettings", "ProjectVersion.txt");
            if (File.Exists(projectVersionFile))
            {
                return candidate;
            }
        }

        // Check environment variable
        var envProject = Environment.GetEnvironmentVariable("UNITY_PROJECT");
        if (!string.IsNullOrEmpty(envProject) && Directory.Exists(envProject))
        {
            return envProject;
        }

        return null;
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
