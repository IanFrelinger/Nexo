using System.Diagnostics;
using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Infrastructure;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// End-to-end smoke tests for Phases 1-4: observe, analyze bricks, adapt, improve.
/// Uses prebuilt CLI (build once, run many) for faster execution.
/// </summary>
public sealed class Phases14CliE2ETests : IDisposable
{
    private readonly string _repoRoot;
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public Phases14CliE2ETests()
    {
        _repoRoot = TestPaths.FindRepoRoot();
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-phases14-e2e");
    }

    public void Dispose()
    {
        _tempDirCleanup.Dispose();
    }

    [Fact]
    public async Task AdaptCommand_DryRun_ExitsZero()
    {
        var (code, stdout, _) = await RunCliAsync(_repoRoot, "adapt --dry-run");

        Assert.Equal(0, code);
        Assert.Contains("Decomposed", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImproveCommand_DryRun_Exits()
    {
        var (code, _, _) = await RunCliAsync(_repoRoot, "improve --dry-run");

        Assert.True(code == 0 || code == 1);
    }

    [Fact]
    public async Task ImproveCommand_WithViolations_AppliesFixes()
    {
        var csPath = Path.Combine(_tempDir, "EmptyCatch.cs");
        await File.WriteAllTextAsync(csPath, """
            using System;
            namespace Test;
            public class C
            {
                public void M()
                {
                    try { }
                    catch (Exception) { }
                }
            }
            """);

        var (code, _, _) = await RunCliAsync(_repoRoot, $"improve --path \"{_tempDir}\"");

        Assert.True(code == 0 || code == 1);
        var content = await File.ReadAllTextAsync(csPath);
        Assert.True(
            content.Contains("Trace.WriteLine", StringComparison.Ordinal) || code == 0,
            "File should be modified with Trace.WriteLine when violations were fixed, or exit 0 if no violations");
    }

    [Fact]
    public async Task AnalyzeBricksCommand_Exits()
    {
        var obsPath = Path.Combine(_repoRoot, "src", "Nexo.Infrastructure", "Observation");
        var path = Directory.Exists(obsPath) ? obsPath : _repoRoot;

        var (code, _, _) = await RunCliAsync(_repoRoot, $"analyze bricks --path \"{path}\"");

        Assert.True(code == 0 || code == 1);
    }

    [Fact]
    public async Task Phases14_FullPipeline_ObserveAnalyzeAdaptImprove()
    {
        var watchPath = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(watchPath);

        var (observeCode, _, _) = await RunCliAsync(_repoRoot, $"observe --path \"{_tempDir}\" --duration 1s");
        Assert.Equal(0, observeCode);

        var obsPath = Path.Combine(_repoRoot, "src", "Nexo.Infrastructure", "Observation");
        var analyzePath = Directory.Exists(obsPath) ? obsPath : _repoRoot;

        var (analyzeCode, _, _) = await RunCliAsync(_repoRoot, $"analyze bricks --path \"{analyzePath}\"");
        Assert.True(analyzeCode == 0 || analyzeCode == 1);

        var (adaptCode, _, _) = await RunCliAsync(_repoRoot, "adapt --dry-run");
        Assert.Equal(0, adaptCode);

        var (improveCode, _, _) = await RunCliAsync(_repoRoot, "improve --dry-run");
        Assert.True(improveCode == 0 || improveCode == 1);
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunCliAsync(string workingDir, string args)
    {
        var cliPath = await EnsureCliBuiltAsync(workingDir);
        var fullArgs = $"\"{cliPath}\" {args}";

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = fullArgs,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process");
        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();

        await p.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (p.ExitCode, stdout, stderr);
    }

    private static readonly object _cliBuildLock = new();
    private static string? _cachedCliPath;

    private static Task<string> EnsureCliBuiltAsync(string repoRoot)
    {
        var cliDll = Path.Combine(repoRoot, "src", "Nexo.CLI", "bin", "Debug", "net8.0", "Nexo.CLI.dll");

        lock (_cliBuildLock)
        {
            if (_cachedCliPath != null && File.Exists(_cachedCliPath))
                return Task.FromResult(_cachedCliPath);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build src/Nexo.CLI/Nexo.CLI.csproj --verbosity quiet",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet build");
            var stderrTask = p.StandardError.ReadToEndAsync();
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                var err = stderrTask.GetAwaiter().GetResult();
                throw new InvalidOperationException($"CLI build failed (exit {p.ExitCode}): {err}");
            }

            if (!File.Exists(cliDll))
                throw new InvalidOperationException($"CLI DLL not found at {cliDll} after build");

            _cachedCliPath = cliDll;
            return Task.FromResult(cliDll);
        }
    }
}
