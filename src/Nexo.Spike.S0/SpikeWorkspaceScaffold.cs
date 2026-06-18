using System.Diagnostics;
using System.Text.Json;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S0.Roles;

namespace Nexo.Spike.S0;

/// <summary>
/// Materialises a fresh brick workspace from the spike-s0 template.
/// </summary>
public static class SpikeWorkspaceScaffold
{
    public const string TestProject = "CsvColumnInferrer.Tests/CsvColumnInferrer.Tests.csproj";
    public const string ImplProject = "CsvColumnInferrer/CsvColumnInferrer.csproj";
    public static string ResolveTemplateRoot()
    {
        var candidates = new List<string>();

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            candidates.Add(Path.Combine(dir.FullName, "samples", "spike-s0", "template"));
            dir = dir.Parent;
        }

        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "samples", "spike-s0", "template"));

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException(
            "Could not locate samples/spike-s0/template. Run from repository root.");
    }

    public static void CreateFresh(string workspaceRoot, bool overwrite = false)
    {
        if (Directory.Exists(workspaceRoot))
        {
            if (!overwrite)
                throw new InvalidOperationException($"Workspace already exists: {workspaceRoot}");
            Directory.Delete(workspaceRoot, recursive: true);
        }

        CopyDirectory(ResolveTemplateRoot(), workspaceRoot);
        Directory.CreateDirectory(Path.Combine(workspaceRoot, ".stage"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "prompts", "test-author"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "prompts", "implementer"));
    }

    public static void CleanBuildArtifacts(string workspaceRoot)
    {
        foreach (var dir in new[] { "bin", "obj" })
        {
            var impl = Path.Combine(workspaceRoot, "CsvColumnInferrer", dir);
            var tests = Path.Combine(workspaceRoot, "CsvColumnInferrer.Tests", dir);
            var properties = Path.Combine(workspaceRoot, "CsvColumnInferrer.Properties", dir);
            if (Directory.Exists(impl))
                Directory.Delete(impl, recursive: true);
            if (Directory.Exists(tests))
                Directory.Delete(tests, recursive: true);
            if (Directory.Exists(properties))
                Directory.Delete(properties, recursive: true);
        }
    }

    public static async Task<(int exitCode, string stdout, string stderr, bool timedOut)> BuildAsync(
        string workspaceRoot,
        CancellationToken ct = default) =>
        await SpikeDotnetRunner.RunAsync(
            workspaceRoot,
            $"build \"{TestProject}\" -c Release",
            TimeSpan.FromMinutes(10),
            ct).ConfigureAwait(false);

    public static async Task<(int exitCode, string stdout, string stderr, bool timedOut)> TestAsync(
        string workspaceRoot,
        CancellationToken ct = default) =>
        await SpikeDotnetRunner.RunAsync(
            workspaceRoot,
            $"test \"{TestProject}\" -c Release --logger trx --no-build --blame-hang-timeout 60s --blame-hang-dump-type none --verbosity minimal",
            TimeSpan.FromMinutes(10),
            ct).ConfigureAwait(false);

    /// <summary>Runs tests with an implicit rebuild — use after implementation changes at GREEN.</summary>
    public static async Task<(int exitCode, string stdout, string stderr, bool timedOut)> TestWithRebuildAsync(
        string workspaceRoot,
        CancellationToken ct = default) =>
        await SpikeDotnetRunner.RunAsync(
            workspaceRoot,
            $"test \"{TestProject}\" -c Release --logger trx --blame-hang-timeout 60s --blame-hang-dump-type none --verbosity minimal",
            TimeSpan.FromMinutes(10),
            ct).ConfigureAwait(false);

    public static async Task CommitStageAsync(string workspaceRoot, SpikeStage stage, CancellationToken ct = default)
    {
        var message = stage switch
        {
            SpikeStage.Spec => "spike-s0: frozen spec",
            SpikeStage.Red => "spike-s0: frozen tests",
            SpikeStage.Green => "spike-s0: implementation",
            SpikeStage.Verify => "spike-s0: verify",
            _ => $"spike-s0: {stage}"
        };

        await RunGitAsync(workspaceRoot, "add -A", ct).ConfigureAwait(false);
        await RunGitAsync(workspaceRoot, $"commit -m \"{message}\" --allow-empty", ct).ConfigureAwait(false);
    }

    public static async Task InitGitAsync(string workspaceRoot, CancellationToken ct = default)
    {
        if (Directory.Exists(Path.Combine(workspaceRoot, ".git")))
            return;

        await RunGitAsync(workspaceRoot, "init", ct).ConfigureAwait(false);
        await RunGitAsync(workspaceRoot, "add -A", ct).ConfigureAwait(false);
        await RunGitAsync(workspaceRoot, "commit -m \"spike-s0: scaffold\"", ct).ConfigureAwait(false);
    }

    public static async Task WriteRunLogAsync(string workspaceRoot, SpikeRunLog log, CancellationToken ct = default)
    {
        var path = Path.Combine(workspaceRoot, "spike-run-log.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            log,
            new JsonSerializerOptions { WriteIndented = true },
            ct).ConfigureAwait(false);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);

        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }

    private static async Task RunGitAsync(string workspaceRoot, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            WorkingDirectory = workspaceRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;
        await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
    }
}
