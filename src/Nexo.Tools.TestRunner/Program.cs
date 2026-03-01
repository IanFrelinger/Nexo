using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nexo.Tools.TestRunner;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Check if we should run agent smoke tests
        if (args.Length > 0 && args[0] == "--agent-smoke-tests")
        {
            return await RunAgentSmokeTests.RunAsync(args.Skip(1).ToArray());
        }
        
        // Check if we should run CLI demo E2E tests
        if (args.Length > 0 && args[0] == "--cli-demo-e2e")
        {
            return await RunCliDemoE2ETests.RunAsync(args.Skip(1).ToArray());
        }

        // Check if we should run GameAdapter mock plugin tests
        if (args.Length > 0 && args[0] == "--game-adapter-mock")
        {
            return await RunGameAdapterMockTests.RunAsync(args.Skip(1).ToArray());
        }

        var logger = new ConsoleLogger();
        logger.LogInformation("Nexo CLI Unit Test Runner (Cross-Platform)");
        logger.LogInformation("==============================================");
        logger.LogInformation("");

        var context = TestContext.Create(logger);
        Directory.CreateDirectory(context.ResultsDir);

        var (requestedTargets, includeMobile, strict) = ArgumentParser.Parse(args, logger);
        var registry = RunnerRegistry.Build(includeMobile, context);
        var targets = TargetSelector.ResolveTargets(requestedTargets, includeMobile, registry).ToList();

        if (targets.Count == 0)
        {
            logger.LogError("No platforms requested. Use --platform to specify targets.");
            return 1;
        }

        var results = new List<TestRunResult>();
        foreach (var target in targets)
        {
            var runner = registry.Resolve(target);
            if (runner is null)
            {
                results.Add(strict
                    ? TestRunResult.Failure(target, $"Unknown or unsupported platform '{target}'")
                    : TestRunResult.SkippedResult(target, $"Unknown or unsupported platform '{target}'"));
                continue;
            }

            if (!runner.IsEnabled)
            {
                results.Add(strict
                    ? TestRunResult.Failure(target, $"Platform '{target}' is not supported on this host")
                    : TestRunResult.SkippedResult(target, $"Platform '{target}' is not supported on this host"));
                continue;
            }

            try
            {
                logger.LogInformation($"Testing on {target}...");
                logger.LogInformation("");
                results.Add(await runner.RunAsync());
                logger.LogInformation("");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error testing {target}: {ex.Message}");
                results.Add(TestRunResult.Failure(target, ex.Message));
            }
        }

        return SummaryPrinter.Render(results, context.ResultsDir, logger);
    }
}

#region Argument & target helpers

static class ArgumentParser
{
    public static (List<string> requestedTargets, bool includeMobile, bool strict) Parse(string[] args, ILogger logger)
    {
        var targets = new List<string>();
        var includeMobile = false;
        var strict = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--all":
                    targets.Add("ubuntu");
                    break;
                case "--mobile":
                    includeMobile = true;
                    break;
                case "--strict":
                    strict = true;
                    break;
                case "--no-strict":
                    strict = false;
                    break;
                case "--quick":
                    // Reserved for future quick mode support.
                    break;
                case "--platform":
                    if (i + 1 < args.Length)
                    {
                        targets.Add(args[++i]);
                    }
                    break;
                default:
                    logger.LogError($"Unknown option: {args[i]}");
                    PrintUsage(logger);
                    Environment.Exit(1);
                    break;
            }
        }

        if (targets.Count == 0)
        {
            targets.Add("ubuntu");
        }

        return (targets, includeMobile, strict);
    }

    private static void PrintUsage(ILogger logger)
    {
        logger.LogInformation("Usage: dotnet run -- [options]");
        logger.LogInformation("");
        logger.LogInformation("Options:");
        logger.LogInformation("  --all              Run on default desktop platform(s)");
        logger.LogInformation("  --mobile           Include mobile platforms (iOS, Android)");
        logger.LogInformation("  --strict           Treat unsupported platforms as failures (default in CI)");
        logger.LogInformation("  --no-strict        Skip unsupported platforms (default locally)");
        logger.LogInformation("  --quick            Skip build cache (reserved)");
        logger.LogInformation("  --platform <name>  Run on specific platform (ubuntu, ios, android)");
        logger.LogInformation("");
        logger.LogInformation("Examples:");
        logger.LogInformation("  dotnet run --                    # Linux only");
        logger.LogInformation("  dotnet run -- --mobile           # Linux + iOS + Android");
        logger.LogInformation("  dotnet run -- --platform ios     # iOS only");
        logger.LogInformation("  dotnet run -- --platform android # Android only");
        logger.LogInformation("");
    }
}

static class TargetSelector
{
    public static IEnumerable<string> ResolveTargets(IEnumerable<string> requested, bool includeMobile, RunnerRegistry registry)
    {
        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var target in requested)
        {
            if (seen.Add(target))
            {
                ordered.Add(target);
            }
        }

        if (includeMobile)
        {
            foreach (var runner in registry.MobileRunners)
            {
                var id = runner.PlatformIds.First();
                if (seen.Add(id))
                {
                    ordered.Add(id);
                }
            }
        }

        return ordered;
    }
}

static class SummaryPrinter
{
    public static int Render(IEnumerable<TestRunResult> results, string resultsDir, ILogger logger)
    {
        var passed = results.Where(r => r.Status == TestRunStatus.Passed).ToList();
        var skipped = results.Where(r => r.Status == TestRunStatus.Skipped).ToList();
        var failed = results.Where(r => r.Status == TestRunStatus.Failed).ToList();

        logger.LogInformation("==============================================");
        logger.LogInformation("Test Summary");
        logger.LogInformation("==============================================");

        if (passed.Count > 0)
        {
            logger.LogInformation("Passed platforms:");
            foreach (var result in passed)
            {
                logger.LogInformation($"   - {result.Platform}");
            }
        }

        if (skipped.Count > 0)
        {
            logger.LogInformation("Skipped platforms:");
            foreach (var result in skipped)
            {
                logger.LogInformation($"   - {result.Platform} - {result.Message}");
            }
        }

        if (failed.Count > 0)
        {
            logger.LogError("Failed platforms:");
            foreach (var result in failed)
            {
                logger.LogError($"   - {result.Platform} - {result.Message}");
                if (!string.IsNullOrEmpty(result.LogPath))
                {
                    logger.LogInformation($"   Logs: {result.LogPath}");
                }
            }
            return 1;
        }

        logger.LogInformation("");
        logger.LogInformation(skipped.Count > 0
            ? "All supported platform tests passed (some platforms were skipped)!"
            : "All tests passed on all platforms!");
        logger.LogInformation("");
        logger.LogInformation($"Test results are available in: {resultsDir}");
        return 0;
    }
}

#endregion

#region Context & runtime info

record TestContext(string ProjectRoot, string ResultsDir, IRuntimeInfo RuntimeInfo, ILogger Logger)
{
    public static TestContext Create(ILogger logger)
    {
        var root = DiscoverProjectRoot();
        var results = Path.Combine(root, "test-results");
        return new TestContext(root, results, SystemRuntimeInfo.Create(), logger);
    }

    private static string DiscoverProjectRoot()
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
}

interface IRuntimeInfo
{
    bool IsWindows { get; }
    bool IsMacOS { get; }
    bool IsLinux { get; }
}

sealed class SystemRuntimeInfo : IRuntimeInfo
{
    private SystemRuntimeInfo(bool isWindows, bool isMac, bool isLinux)
    {
        IsWindows = isWindows;
        IsMacOS = isMac;
        IsLinux = isLinux;
    }

    public bool IsWindows { get; }
    public bool IsMacOS { get; }
    public bool IsLinux { get; }

    public static SystemRuntimeInfo Create() => new(
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
}

#endregion

#region Runner registry & contracts

interface ITestPlatformRunner
{
    IReadOnlyCollection<string> PlatformIds { get; }
    bool IsMobile { get; }
    bool IsEnabled { get; }
    Task<TestRunResult> RunAsync();
}

sealed class RunnerRegistry
{
    private readonly List<ITestPlatformRunner> _runners;
    private readonly Dictionary<string, ITestPlatformRunner> _lookup;

    private RunnerRegistry(IEnumerable<ITestPlatformRunner> runners)
    {
        _runners = runners.ToList();
        _lookup = _runners
            .SelectMany(r => r.PlatformIds.Select(id => (id, runner: r)))
            .ToDictionary(k => k.id, v => v.runner, StringComparer.OrdinalIgnoreCase);
    }

    public static RunnerRegistry Build(bool includeMobile, TestContext context)
    {
        var runners = new List<ITestPlatformRunner>
        {
            new UbuntuDockerRunner(context)
        };

        if (includeMobile)
        {
            runners.Add(new AndroidDockerRunner(context));
            runners.Add(new IosNativeRunner(context));
        }

        return new RunnerRegistry(runners);
    }

    public IEnumerable<ITestPlatformRunner> MobileRunners => _runners.Where(r => r.IsMobile);
    public ITestPlatformRunner? Resolve(string id) => _lookup.TryGetValue(id, out var runner) ? runner : null;
}

#endregion

#region Runner implementations

abstract class DockerRunnerBase : ITestPlatformRunner
{
    protected DockerRunnerBase(TestContext context, string primaryId, IEnumerable<string>? aliases, string dockerfileRelativePath, bool isMobile)
    {
        Context = context;
        DockerfilePath = Path.Combine(context.ProjectRoot, dockerfileRelativePath);
        IsMobile = isMobile;
        PlatformIds = new ReadOnlyCollection<string>(
            new[] { primaryId }.Concat(aliases ?? Array.Empty<string>()).ToList());
    }

    protected TestContext Context { get; }
    protected string DockerfilePath { get; }
    protected string ContainerTag => $"nexo-test-{PlatformIds.First()}";

    public IReadOnlyCollection<string> PlatformIds { get; }
    public bool IsMobile { get; }
    public virtual bool IsEnabled => File.Exists(DockerfilePath) && DockerAvailability.IsUsable();

    public async Task<TestRunResult> RunAsync()
    {
        Directory.CreateDirectory(Context.ResultsDir);
        Context.Logger.LogInformation($"Running tests in Docker ({PlatformIds.First()})...");

        var buildResult = await ProcessRunner.RunAsync(new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"build -f \"{DockerfilePath}\" -t {ContainerTag} .",
            WorkingDirectory = Context.ProjectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        if (buildResult.ExitCode != 0)
        {
            return TestRunResult.Failure(PlatformIds.First(), $"Docker build failed: {buildResult.StandardError}");
        }

        var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"nexo-tests-{Guid.NewGuid():N}"));
        try
        {
            var containerResults = "/workspace/test-results/results.json";
            var containerLogs = "/workspace/test-results/logs.txt";
            var pythonScriptName = "extract.py";
            var shellScriptName = "run.sh";

            await File.WriteAllTextAsync(Path.Combine(tempDir.FullName, pythonScriptName),
                TestScriptBuilder.CreateExtractorScript(containerResults, PlatformIds.First()));

            var shellScript = BuildShellScript(containerResults, containerLogs, pythonScriptName);
            var shellPath = Path.Combine(tempDir.FullName, shellScriptName);
            await File.WriteAllTextAsync(shellPath, shellScript);
            MakeExecutable(shellPath);

            var runArgs = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = BuildDockerArguments(tempDir.FullName, shellScriptName),
                WorkingDirectory = Context.ProjectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var runResult = await ProcessRunner.RunAsync(runArgs);
            Context.Logger.LogDebug(runResult.StandardOutput);
            if (!string.IsNullOrWhiteSpace(runResult.StandardError))
            {
                Context.Logger.LogWarning(runResult.StandardError);
            }

            var finalResults = Path.Combine(Context.ResultsDir, $"{PlatformIds.First()}-results.json");
            var finalLogs = Path.Combine(Context.ResultsDir, $"{PlatformIds.First()}-logs.txt");
            MoveIfExists(Path.Combine(Context.ResultsDir, "results.json"), finalResults);
            MoveIfExists(Path.Combine(Context.ResultsDir, "logs.txt"), finalLogs);

            return runResult.ExitCode == 0
                ? TestRunResult.SuccessResult(PlatformIds.First(), "Tests passed", finalLogs, finalResults)
                : TestRunResult.Failure(PlatformIds.First(), "Tests failed", finalLogs);
        }
        finally
        {
            TryDelete(tempDir.FullName);
        }
    }

    protected virtual string BuildDockerArguments(string tempDir, string scriptName)
    {
        var envVariables = string.Join(" ", GetEnvironmentVariables().Select(kv => $"-e {kv.Key}={kv.Value}"));
        return $"run --rm -v \"{Context.ResultsDir}:/workspace/test-results\" -v \"{tempDir}:/scripts\" {envVariables} {ContainerTag} bash /scripts/{scriptName}";
    }

    protected virtual IDictionary<string, string> GetEnvironmentVariables() => new Dictionary<string, string>();
    protected abstract string BuildShellScript(string resultsPath, string logsPath, string pythonScriptName);

    private static void MoveIfExists(string source, string destination)
    {
        if (File.Exists(source))
        {
            File.Move(source, destination, true);
        }
    }

    private static void MakeExecutable(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "chmod",
            Arguments = $"+x \"{path}\"",
            UseShellExecute = false
        })?.WaitForExit();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // ignore cleanup issues
        }
    }
}

static class DockerAvailability
{
    public static bool IsUsable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var p = Process.Start(psi);
            if (p == null) return false;

            if (!p.WaitForExit(2000))
            {
                try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return false;
            }

            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

sealed class UbuntuDockerRunner : DockerRunnerBase
{
    public UbuntuDockerRunner(TestContext context)
        : base(context, "ubuntu", new[] { "linux" }, ".docker/Dockerfile.test", isMobile: false)
    {
    }

    protected override string BuildShellScript(string resultsPath, string logsPath, string pythonScriptName)
    {
        return $"#!/bin/bash{Environment.NewLine}" +
               "cd src/Nexo.CLI" + Environment.NewLine +
               $"dotnet run --project Nexo.CLI.csproj -- test --format-json > {resultsPath} 2>{logsPath} || true" + Environment.NewLine +
               $"python3 /scripts/{pythonScriptName}{Environment.NewLine}";
    }
}

sealed class AndroidDockerRunner : DockerRunnerBase
{
    public AndroidDockerRunner(TestContext context)
        : base(context, "android", null, ".docker/Dockerfile.test-android", isMobile: true)
    {
    }

    protected override IDictionary<string, string> GetEnvironmentVariables()
        => new Dictionary<string, string> { { "ANDROID_HOME", "/opt/android-sdk" } };

    protected override string BuildShellScript(string resultsPath, string logsPath, string pythonScriptName)
    {
        return $"#!/bin/bash{Environment.NewLine}" +
               "cd src/Nexo.CLI" + Environment.NewLine +
               $"dotnet run --project Nexo.CLI.csproj -- test --format-json > {resultsPath} 2>{logsPath} || true" + Environment.NewLine +
               $"python3 /scripts/{pythonScriptName}{Environment.NewLine}";
    }
}

sealed class IosNativeRunner : ITestPlatformRunner
{
    private readonly TestContext _context;

    public IosNativeRunner(TestContext context)
    {
        _context = context;
        PlatformIds = new ReadOnlyCollection<string>(new[] { "ios" });
    }

    public IReadOnlyCollection<string> PlatformIds { get; }
    public bool IsMobile => true;
    public bool IsEnabled => _context.RuntimeInfo.IsMacOS;

    public async Task<TestRunResult> RunAsync()
    {
        _context.Logger.LogInformation("Running tests on iOS (macOS native)...");

        var xcodeResult = await ProcessRunner.RunAsync(new ProcessStartInfo
        {
            FileName = "xcodebuild",
            Arguments = "-version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        if (xcodeResult.ExitCode != 0)
        {
            return TestRunResult.Failure("ios", "Xcode is not installed");
        }

        var cliProject = Path.Combine(_context.ProjectRoot, "src", "Nexo.CLI", "Nexo.CLI.csproj");
        if (!File.Exists(cliProject))
        {
            return TestRunResult.Failure("ios", $"CLI project not found at {cliProject}");
        }

        var logsFile = Path.Combine(_context.ResultsDir, "ios-logs.txt");
        var resultsFile = Path.Combine(_context.ResultsDir, "ios-results.json");

        var testResult = await ProcessRunner.RunAsync(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project Nexo.CLI.csproj -- test --format-json",
            WorkingDirectory = Path.Combine(_context.ProjectRoot, "src", "Nexo.CLI"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        await File.WriteAllTextAsync(logsFile, testResult.StandardError);
        var parsed = TestResultParser.TryParse(testResult.StandardOutput);
        if (parsed == null)
        {
            return TestRunResult.Failure("ios", "Unable to parse test output", logsFile);
        }

        var json = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(resultsFile, json);
        var message = $"iOS: {parsed.TotalTests} total, {parsed.PassedTests} passed, {parsed.FailedTests} failed";

        return parsed.FailedTests == 0
            ? TestRunResult.SuccessResult("ios", message, logsFile, resultsFile)
            : TestRunResult.Failure("ios", message, logsFile);
    }
}

#endregion

#region Result & parsing helpers

record TestRunResult(string Platform, TestRunStatus Status, string Message, string? LogPath, string? ResultPath)
{
    public static TestRunResult SuccessResult(string platform, string message, string? logPath = null, string? resultPath = null)
        => new(platform, TestRunStatus.Passed, message, logPath, resultPath);

    public static TestRunResult Failure(string platform, string message, string? logPath = null)
        => new(platform, TestRunStatus.Failed, message, logPath, null);

    public static TestRunResult SkippedResult(string platform, string message)
        => new(platform, TestRunStatus.Skipped, message, null, null);
}

enum TestRunStatus
{
    Passed,
    Failed,
    Skipped
}

static class TestResultParser
{
    public static TestResults? TryParse(string output)
    {
        var jsonMatch = Regex.Match(output, @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", RegexOptions.Singleline);
        if (jsonMatch.Success)
        {
            try
            {
                var json = jsonMatch.Value;
                var result = JsonSerializer.Deserialize<TestResults>(json);
                if (result != null && result.TotalTests > 0)
                {
                    return result;
                }
            }
            catch
            {
                // ignore
            }
        }

        var passedMatch = Regex.Match(output, @"(\d+)\s+passed", RegexOptions.IgnoreCase);
        var failedMatch = Regex.Match(output, @"(\d+)\s+failed", RegexOptions.IgnoreCase);
        var totalMatch = Regex.Match(output, @"(\d+)\s+total", RegexOptions.IgnoreCase);

        if (passedMatch.Success && failedMatch.Success)
        {
            var passed = int.Parse(passedMatch.Groups[1].Value);
            var failed = int.Parse(failedMatch.Groups[1].Value);
            var total = totalMatch.Success ? int.Parse(totalMatch.Groups[1].Value) : passed + failed;

            return new TestResults
            {
                TotalTests = total,
                PassedTests = passed,
                FailedTests = failed
            };
        }

        return null;
    }

    public class TestResults
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
    }
}

static class TestScriptBuilder
{
    private const string Template = @"
import json
import sys
import re

RESULT_PATH = r""{RESULT_PATH}""
PLATFORM = ""{PLATFORM}""

try:
    with open(RESULT_PATH, 'r') as f:
        content = f.read()
    matches = re.findall(r'\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}', content, re.DOTALL)
    for match in reversed(matches):
        try:
            data = json.loads(match)
            if 'TotalTests' in data:
                with open(RESULT_PATH, 'w') as out:
                    json.dump(data, out, indent=2)
                total = data.get('TotalTests', 0)
                passed = data.get('PassedTests', 0)
                failed = data.get('FailedTests', 0)
                print(f'{PLATFORM}: {total} total, {passed} passed, {failed} failed')
                sys.exit(0 if failed == 0 else 1)
        except Exception:
            continue
    print('No valid test results found')
    sys.exit(1)
except Exception as e:
    print(f'Error: {{e}}')
    sys.exit(1)
";

    public static string CreateExtractorScript(string resultPath, string platformName)
    {
        var escapedPath = resultPath.Replace(@"\", @"\\");
        return Template
            .Replace("{RESULT_PATH}", escapedPath)
            .Replace("{PLATFORM}", platformName);
    }
}

static class ProcessRunner
{
    public static async Task<ProcessRunResult> RunAsync(ProcessStartInfo info)
    {
        using var process = Process.Start(info);
        if (process == null)
        {
            return new ProcessRunResult(-1, string.Empty, "Failed to start process");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new ProcessRunResult(process.ExitCode, await outputTask, await errorTask);
    }
}

record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);

#endregion
