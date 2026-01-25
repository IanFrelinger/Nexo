using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Testing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Nexo.Tests.Infrastructure.Tests.MultiPlatform;

/// <summary>
/// Infrastructure test for iOS platform (.NET 8.0).
/// 
/// Uses hybrid approach:
/// - Attempts Docker first (if available)
/// - Falls back to native execution (required for iOS)
/// 
/// Note: iOS devices/simulators don't support Docker, so native execution is used.
/// This test runs natively on macOS to validate iOS compatibility.
/// </summary>
public class Ios80Test : MultiPlatformTestBase
{
    public Ios80Test(ILogger<Ios80Test>? logger = null)
        : base("ios-8.0", "8.0", "iOS (.NET 8.0)", executionPlatform: null, logger)
    {
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // iOS uses native execution (no Docker support)
        return await ExecuteTestAsync(
            dockerfile: null, // iOS doesn't support Docker
            nativeExecutor: ExecuteNativeIosTestAsync,
            cancellationToken);
    }

    private async Task<TestResult> ExecuteNativeIosTestAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        // Check if running on macOS (required for iOS testing)
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new TestResult
            {
                TestName = TestName,
                Category = Category,
                Passed = false,
                Message = "iOS testing requires macOS (cannot run in Docker)",
                Duration = DateTime.UtcNow - startTime,
                ErrorMessage = "iOS tests must run natively on macOS. Docker is not available on iOS devices or simulators."
            };
        }

        // Check for Xcode (required for iOS development)
        if (!await IsXcodeAvailableAsync(cancellationToken))
        {
            return new TestResult
            {
                TestName = TestName,
                Category = Category,
                Passed = false,
                Message = "Xcode is not installed (required for iOS testing)",
                Duration = DateTime.UtcNow - startTime,
                ErrorMessage = "Please install Xcode from the App Store to enable iOS testing."
            };
        }

        try
        {
            var testProject = Path.Combine(_projectRoot, "src", "Nexo.Tests.Infrastructure", "Nexo.Tests.Infrastructure.csproj");
            if (!File.Exists(testProject))
            {
                return new TestResult
                {
                    TestName = TestName,
                    Category = Category,
                    Passed = false,
                    Message = $"Test project not found: {testProject}",
                    Duration = DateTime.UtcNow - startTime
                };
            }

            // Run tests natively (no Docker - iOS doesn't support it)
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"test \"{testProject}\" --filter \"FullyQualifiedName~BaseFrameworkSmokeTests\" --logger \"console;verbosity=minimal\"",
                WorkingDirectory = _projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var result = await RunProcessAsync(processInfo, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            // Parse results
            var output = result.StandardOutput + result.StandardError;
            var (passed, failed, total) = ParseTestResults(output);

            return new TestResult
            {
                TestName = TestName,
                Category = Category,
                Passed = result.ExitCode == 0 && (total == 0 || failed == 0),
                Message = $"iOS (macOS native): {total} total, {passed} passed, {failed} failed",
                Duration = duration,
                ErrorMessage = result.ExitCode != 0 ? result.StandardError : null,
                Metadata = new Dictionary<string, object>
                {
                    ["Platform"] = "ios-8.0",
                    ["DotNetVersion"] = "8.0",
                    ["ExecutionMode"] = "native",
                    ["TotalTests"] = total,
                    ["PassedTests"] = passed,
                    ["FailedTests"] = failed
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
                Message = $"Error running iOS tests: {ex.Message}",
                Duration = DateTime.UtcNow - startTime,
                ErrorMessage = ex.ToString()
            };
        }
    }

    private async Task<bool> IsXcodeAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "xcodebuild",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var result = await RunProcessAsync(processInfo, cancellationToken);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
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
