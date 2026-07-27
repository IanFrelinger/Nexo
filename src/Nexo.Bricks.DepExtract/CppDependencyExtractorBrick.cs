using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Computes the forward ("lower") and reverse ("upper") #include dependency
/// closure of one or more C/C++ entry files, using the real compiler's -MM
/// dependency scanner (not text heuristics), and produces a standalone
/// duplicate of the entry plus its dependencies. Delegates to the `dep-extract`
/// Docker image — a container with no network access.
/// </summary>
public sealed class CppDependencyExtractorBrick : DomainBrick
{
    private readonly ILogger<CppDependencyExtractorBrick> _logger;

    public CppDependencyExtractorBrick(ILogger<CppDependencyExtractorBrick> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "cpp-dependency-extractor";
        Name = "C++ Dependency Extractor";
        Version = "1.1.0";
        Icon = "🧩";
        Category = BrickCategory.Analysis;
        Description = "Computes the compiler-driven #include dependency closure of C/C++ entry " +
                      "files (forward + reverse) and produces a standalone duplicate. Runs fully " +
                      "offline in a sandboxed container; nothing mounted is ever transmitted.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("srcDir", "string", "Absolute host path to the source tree, mounted read-only"),
                new BrickInputDefinition("outDir", "string", "Absolute host path where the manifest/duplicate are written"),
                new BrickInputDefinition("entries", "string[]", "Entry file path(s), relative to srcDir"),
                new BrickInputDefinition("scanDir", "string", "Subtree (relative to srcDir) to scan for reverse dependents", required: false, defaultValue: "."),
                new BrickInputDefinition("includeDirs", "string[]", "Extra -I include roots, relative to srcDir", required: false),
                new BrickInputDefinition("defines", "string[]", "Preprocessor -D defines, as KEY=VAL", required: false),
                new BrickInputDefinition("includeUpper", "bool", "Also copy the upper closure into the duplicate", required: false, defaultValue: false),
                new BrickInputDefinition("manifestOnly", "bool", "Skip producing the duplicate; report only", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("lower", "string[]", "Dependency closure the entry needs to compile"),
                new BrickOutputDefinition("upper", "string[]", "Files that transitively include the entry"),
                new BrickOutputDefinition("external", "string[]", "Entry dependencies that resolve outside srcDir"),
                new BrickOutputDefinition("manifestText", "string", "Full human-readable manifest"),
                new BrickOutputDefinition("duplicatePath", "string", "Host path to the standalone duplicate (absent if manifestOnly)"),
                new BrickOutputDefinition("errorCode", "string", "Structured error code when the brick throws DepExtractException (not set on success)")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "dep-extract-docker",
                Name = "dep-extract (dockerized g++ -MM -MG scan)",
                Description = "Shells out to the dep-extract container image; compiler-driven, no text heuristics, no network.",
                Executor = "ProcessExecutor",
                Config = new Dictionary<string, object> { ["image"] = "dep-extract" },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "seconds, scales with tree size",
                    Deterministic = true,
                    RequiresNetwork = false,
                    ResourceUsage = ResourceUsage.Medium
                }
            }
        };
        DefaultImplementation = ImplementationType.Deterministic;
        FallbackChain = [ImplementationType.Deterministic];

        Metadata = new BrickMetadata
        {
            Author = "evtx-toolkit",
            License = "MIT"
        };
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (implementation is not (ImplementationType.Deterministic or ImplementationType.Auto))
        {
            throw DepExtractException.InvalidInput(
                $"CppDependencyExtractorBrick has no Agentic implementation (requested: {implementation}).");
        }

        var srcDir = DepExtractPathRules.RequireExistingDirectory(input.Get<string>("srcDir"), "srcDir");
        srcDir = DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(srcDir, "srcDir");
        var outDirRaw = input.Get<string>("outDir");
        if (string.IsNullOrWhiteSpace(outDirRaw))
            throw DepExtractException.InvalidInput("outDir is required.");
        var outDir = Path.GetFullPath(outDirRaw);
        outDir = DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(outDir, "outDir");

        var entries = DepExtractPathRules.ResolveEntries(srcDir, input.Get<string[]>("entries") ?? []);
        var scanDir = DepExtractPathRules.ResolveUnderRoot(
            srcDir, input.Get<string?>("scanDir", ".") ?? ".", "scanDir", requireDir: true);
        var includeDirs = (input.Get<string[]?>("includeDirs", null) ?? [])
            .Select(d => DepExtractPathRules.ResolveUnderRoot(srcDir, d, "includeDir", requireDir: true))
            .ToArray();
        var defines = input.Get<string[]?>("defines", null) ?? [];
        foreach (var d in defines)
        {
            if (string.IsNullOrWhiteSpace(d) || d.Contains(' ') || !d.Contains('='))
                throw DepExtractException.InvalidInput($"defines must be KEY=VAL without spaces: '{d}'");
        }
        var includeUpper = input.Get<bool>("includeUpper", false);
        var manifestOnly = input.Get<bool>("manifestOnly", false);

        Directory.CreateDirectory(outDir);

        var args = new List<string>
        {
            "run", "--rm", "--network=none",
            "-v", $"{srcDir}:/src:ro",
            "-v", $"{outDir}:/out"
        };
        if (OperatingSystem.IsLinux())
        {
            var uidGid = await GetHostUidGidAsync(cancellationToken).ConfigureAwait(false);
            if (uidGid is not null) { args.Add("--user"); args.Add(uidGid); }
        }
        args.Add("dep-extract");
        args.Add("--quiet");
        foreach (var e in entries) { args.Add("--entry"); args.Add(e); }
        args.Add("--scan-dir"); args.Add(scanDir);
        foreach (var d in includeDirs) { args.Add("--include-dir"); args.Add(d); }
        foreach (var d in defines) { args.Add("--define"); args.Add(d); }
        if (includeUpper) args.Add("--include-upper");
        if (manifestOnly) args.Add("--manifest-only");

        _logger.LogInformation("Running dep-extract: docker {Args}", string.Join(' ', args));

        string stdout;
        string stderr;
        int exitCode;
        try
        {
            (exitCode, stdout, stderr) = await RunDockerAsync(args, cancellationToken).ConfigureAwait(false);
        }
        catch (DepExtractException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            throw DepExtractException.DockerUnavailable(
                "Failed to start docker. Is the Docker CLI installed and on PATH?", ex);
        }

        if (exitCode != 0)
        {
            var detail = ClassifyDockerFailure(exitCode, stdout, stderr);
            _logger.LogError("dep-extract failed (exit {Code}): {Detail}", exitCode, detail);
            throw DepExtractException.ExtractFailed(detail);
        }

        var lower = ReadLines(outDir, "lower.txt");
        var upper = ReadLines(outDir, "upper.txt");
        var external = ReadLines(outDir, "external.txt");
        var manifestPath = Path.Combine(outDir, "manifest.txt");
        if (!File.Exists(manifestPath))
            throw DepExtractException.ExtractFailed("dep-extract exited 0 but produced no manifest.txt.");
        var manifestText = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        var duplicatePath = Path.Combine(outDir, "duplicate");
        if (!manifestOnly && !Directory.Exists(duplicatePath))
            throw DepExtractException.ExtractFailed("dep-extract exited 0 but produced no duplicate/ directory.");

        var output = new BrickOutput
        {
            Summary = manifestOnly
                ? $"lower={lower.Length} upper={upper.Length} external={external.Length} (manifest only)"
                : $"lower={lower.Length} upper={upper.Length} external={external.Length} duplicate={duplicatePath}"
        };
        output.Set("lower", lower);
        output.Set("upper", upper);
        output.Set("external", external);
        output.Set("manifestText", manifestText);
        if (!manifestOnly) output.Set("duplicatePath", duplicatePath);
        return output;
    }

    private static string[] ReadLines(string dir, string name)
    {
        var path = Path.Combine(dir, name);
        return File.Exists(path)
            ? File.ReadAllLines(path).Where(l => l.Length > 0).ToArray()
            : [];
    }

    private static string ClassifyDockerFailure(int exitCode, string stdout, string stderr)
    {
        var blob = (stderr + "\n" + stdout).Trim();
        if (blob.Contains("Unable to find image", StringComparison.OrdinalIgnoreCase)
            || blob.Contains("pull access denied", StringComparison.OrdinalIgnoreCase))
            return $"dep-extract image missing (exit {exitCode}). Build with: docker build -t dep-extract tools/dep_extract. Detail: {Trim(blob)}";
        if (blob.Contains("Cannot connect to the Docker daemon", StringComparison.OrdinalIgnoreCase)
            || blob.Contains("Is the docker daemon running", StringComparison.OrdinalIgnoreCase))
            return $"Docker daemon unavailable (exit {exitCode}): {Trim(blob)}";
        if (blob.Contains("unknown argument", StringComparison.OrdinalIgnoreCase)
            || blob.Contains("at least one --entry", StringComparison.OrdinalIgnoreCase))
            return $"dep-extract rejected arguments (exit {exitCode}): {Trim(blob)}";
        if (blob.Contains("No such file", StringComparison.OrdinalIgnoreCase)
            || blob.Contains("entry file", StringComparison.OrdinalIgnoreCase))
            return $"dep-extract path/entry error (exit {exitCode}): {Trim(blob)}";
        return $"dep-extract failed with exit code {exitCode}: {Trim(blob)}";
    }

    private static string Trim(string s) =>
        s.Length <= 1200 ? s : s[..1200] + "…";

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunDockerAsync(
        List<string> args, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)
            ?? throw DepExtractException.DockerUnavailable("Failed to start the docker process.");

        var stdoutTask = proc.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = proc.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw;
        }

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        return (proc.ExitCode, stdout, stderr);
    }

    private static async Task<string?> GetHostUidGidAsync(CancellationToken cancellationToken)
    {
        async Task<string?> Run(string arg)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "id",
                    ArgumentList = { arg },
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                if (p is null) return null;
                var outText = await p.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                return p.ExitCode == 0 ? outText.Trim() : null;
            }
            catch { return null; }
        }
        var uid = await Run("-u").ConfigureAwait(false);
        var gid = await Run("-g").ConfigureAwait(false);
        return uid is not null && gid is not null ? $"{uid}:{gid}" : null;
    }
}
