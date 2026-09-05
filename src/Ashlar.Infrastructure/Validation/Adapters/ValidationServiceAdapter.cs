using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Validation.Models;
using Ashlar.Core.Application.Validation.Ports;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Infrastructure.Validation.Parsers;
using System.Diagnostics;

namespace Ashlar.Infrastructure.Validation.Adapters;

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

    /// <summary>Initializes a new validation service adapter.</summary>
    public ValidationServiceAdapter(
        ILogger<ValidationServiceAdapter> logger,
        ITestResultParser testResultParser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _testResultParser = testResultParser ?? throw new ArgumentNullException(nameof(testResultParser));
    }

    /// <summary>Validate asynchronously.</summary>
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
                .Where(f => IsDiscoverableTestProject(f, currentDir))
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
            int totalTestsSkipped = 0;
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
                    // often have no DLL until an explicit build; CI `ashlar validate` previously failed
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

                    // One framework per project: a multi-target project runs a single TFM (net8.0
                    // when it has it) so validate does not double the hosts under load, and a
                    // single-target project runs the TFM it actually declares — forcing net8.0 on
                    // a net9.0-only project produced "The argument <dll> is invalid" from VSTest
                    // for an output that was never built.
                    var framework = SelectTestFramework(csprojPath);
                    var exitCode = await RunDotnetTestForValidateAsync(
                        csprojPath,
                        framework,
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

                        // Skipped tests were not run: they are neither passes nor failures and
                        // stay out of the per-test list (consumers list `!Passed` as failures).
                        // Their count is still reported.
                        var executed = parsedResults.Where(r => !r.Skipped).ToList();
                        var skipped = parsedResults.Count - executed.Count;
                        if (skipped > 0)
                        {
                            _logger.LogInformation(
                                "{Project}: {Skipped} test(s) skipped (not counted as failures)",
                                testProject.Name,
                                skipped);
                        }

                        allTestResults.AddRange(executed);
                        totalTestsRun += executed.Count;
                        totalTestsPassed += executed.Count(r => r.Passed);
                        totalTestsFailed += executed.Count(r => !r.Passed);
                        totalTestsSkipped += skipped;
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

            var skippedSuffix = totalTestsSkipped > 0 ? $", {totalTestsSkipped} skipped" : string.Empty;
            return new ValidationResult
            {
                Passed = passed,
                Message = passed
                    ? $"Validation passed ({totalTestsPassed}/{totalTestsRun} tests{skippedSuffix})"
                    : $"Validation failed ({totalTestsFailed}/{totalTestsRun} tests failed{skippedSuffix})",
                TestsRun = totalTestsRun,
                TestsPassed = totalTestsPassed,
                TestsFailed = totalTestsFailed,
                TestsSkipped = totalTestsSkipped,
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

    /// <summary>
    /// Whether a <c>.csproj</c> under the validation root is a test project we should build and
    /// run. A project qualifies by name or directory ("Test"), and is excluded when it is a
    /// build helper, lives under a hidden directory (<c>.git</c>, <c>.claude</c> worktrees — a
    /// full second checkout would double every run), lives under a <c>templates</c> directory,
    /// or is itself a template whose name still carries a <c>__Placeholder__</c> token — those
    /// exist to be stamped by <c>ashlar new</c>, not compiled in place.
    /// </summary>
    internal static bool IsDiscoverableTestProject(FileInfo project, DirectoryInfo root)
    {
        var name = project.Name;
        var directory = project.DirectoryName ?? string.Empty;

        var looksLikeTests =
            name.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
            directory.Contains("test", StringComparison.OrdinalIgnoreCase);
        if (!looksLikeTests)
            return false;

        if (name.Equals("copy-assemblies.csproj", StringComparison.OrdinalIgnoreCase))
            return false;

        if (PlaceholderToken.IsMatch(name))
            return false;

        // Only the segments below the validation root matter: the root itself may legitimately
        // sit under a hidden or "templates" directory.
        var relative = Path.GetRelativePath(root.FullName, directory);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment.Length == 0 || segment == ".")
                continue;
            if (segment.StartsWith('.'))
                return false;
            if (segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("obj", StringComparison.OrdinalIgnoreCase))
                return false;
            if (segment.Equals("templates", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("template", StringComparison.OrdinalIgnoreCase))
                return false;
            // Author-shaped fixture projects for the cert-gate corpus, not runnable tests.
            if (segment.Equals("adversarial-corpus", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static readonly System.Text.RegularExpressions.Regex PlaceholderToken =
        new("__[A-Za-z0-9]+__", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// The single target framework validate should run for a project, read from the project
    /// file: a multi-target project (<c>TargetFrameworks</c>) runs <c>net8.0</c> when it lists it
    /// (one host, not two, under load), otherwise its first framework; a single-target project
    /// runs the framework it declares. Null when the project declares neither, in which case
    /// <c>dotnet test</c> is left to its own resolution.
    /// </summary>
    internal static string? SelectTestFramework(string csprojPath)
    {
        try
        {
            var doc = System.Xml.Linq.XDocument.Load(csprojPath);
            var multi = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "TargetFrameworks")?.Value;
            if (!string.IsNullOrWhiteSpace(multi))
            {
                var frameworks = multi.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (frameworks.Length == 0)
                    return null;
                return frameworks.FirstOrDefault(f => f.Equals("net8.0", StringComparison.OrdinalIgnoreCase))
                       ?? frameworks[0];
            }

            var single = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "TargetFramework")?.Value?.Trim();
            return string.IsNullOrEmpty(single) ? null : single;
        }
        catch (Exception)
        {
            // An unreadable project file surfaces at build; the framework choice is not the
            // place to fail it.
            return null;
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
    /// Runs <c>dotnet test</c> for validate: one framework (see <see cref="SelectTestFramework"/>),
    /// TRX for parsing, optional console streaming.
    /// </summary>
    private static async Task<int> RunDotnetTestForValidateAsync(
        string csprojPath, string? framework, bool streamOutput, CancellationToken ct)
    {
        var verbosity = streamOutput ? "normal" : "minimal";
        var frameworkArg = framework is null ? string.Empty : $"--framework {framework} ";
        var args =
            $"test \"{csprojPath}\" {frameworkArg}--no-build " +
            "--filter \"Category!=DockerOptional&Category!=Stress\" " +
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
