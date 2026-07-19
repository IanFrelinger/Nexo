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
/// Docker image (tools/dep_extract in the evtx toolkit) — a container with no
/// network access, so this brick is safe to run under air-gapped execution
/// contexts. Deterministic only: there is no LLM reasoning involved, so no
/// Agentic implementation is offered.
/// </summary>
public sealed class CppDependencyExtractorBrick : DomainBrick
{
    private readonly ILogger<CppDependencyExtractorBrick> _logger;

    public CppDependencyExtractorBrick(ILogger<CppDependencyExtractorBrick> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "cpp-dependency-extractor";
        Name = "C++ Dependency Extractor";
        Version = "1.0.0";
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
                new BrickOutputDefinition("duplicatePath", "string", "Host path to the standalone duplicate (absent if manifestOnly)")
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
            throw new ArgumentException(
                $"CppDependencyExtractorBrick has no Agentic implementation (requested: {implementation}).");
        }

        var srcDir = input.Get<string>("srcDir");
        var outDir = input.Get<string>("outDir");
        var entries = input.Get<string[]>("entries");
        var scanDir = input.Get<string?>("scanDir", ".") ?? ".";
        var includeDirs = input.Get<string[]?>("includeDirs", null) ?? [];
        var defines = input.Get<string[]?>("defines", null) ?? [];
        var includeUpper = input.Get<bool>("includeUpper", false);
        var manifestOnly = input.Get<bool>("manifestOnly", false);

        if (entries.Length == 0)
            throw new ArgumentException("At least one entry is required.");
        if (!Directory.Exists(srcDir))
            throw new DirectoryNotFoundException($"srcDir not found: {srcDir}");

        Directory.CreateDirectory(outDir);

        var args = new List<string> { "run", "--rm", "-v", $"{srcDir}:/src:ro", "-v", $"{outDir}:/out" };
        // On native Linux Docker (unlike Docker Desktop's Windows/Mac VM layer), bind-mounted
        // output keeps the CONTAINER's UID on the host filesystem — the image's non-root user,
        // not the caller. Run as the host's own uid:gid so the caller can actually manage its
        // own output (e.g. delete it) afterward.
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

        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start the docker process.");
        var stdoutTask = proc.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = proc.StandardError.ReadToEndAsync(cancellationToken);
        await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        await stdoutTask.ConfigureAwait(false);   // drained; dep-extract's own manifest.txt on disk is authoritative

        if (proc.ExitCode != 0)
        {
            _logger.LogError("dep-extract failed (exit {Code}): {Stderr}", proc.ExitCode, stderr);
            throw new InvalidOperationException($"dep-extract failed with exit code {proc.ExitCode}: {stderr}");
        }

        static string[] ReadLines(string dir, string name)
        {
            var path = Path.Combine(dir, name);
            return File.Exists(path)
                ? File.ReadAllLines(path).Where(l => l.Length > 0).ToArray()
                : [];
        }

        var lower = ReadLines(outDir, "lower.txt");
        var upper = ReadLines(outDir, "upper.txt");
        var external = ReadLines(outDir, "external.txt");
        var manifestPath = Path.Combine(outDir, "manifest.txt");
        var manifestText = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : "";
        var duplicatePath = Path.Combine(outDir, "duplicate");

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

    /// <summary>Returns "uid:gid" for the current host user via `id -u`/`id -g`, or null if unavailable.</summary>
    private static async Task<string?> GetHostUidGidAsync(CancellationToken cancellationToken)
    {
        async Task<string?> Run(string arg)
        {
            var psi = new ProcessStartInfo { FileName = "id", ArgumentList = { arg }, UseShellExecute = false, RedirectStandardOutput = true };
            using var p = Process.Start(psi);
            if (p is null) return null;
            var outText = await p.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return p.ExitCode == 0 ? outText.Trim() : null;
        }
        var uid = await Run("-u").ConfigureAwait(false);
        var gid = await Run("-g").ConfigureAwait(false);
        return uid is not null && gid is not null ? $"{uid}:{gid}" : null;
    }
}
