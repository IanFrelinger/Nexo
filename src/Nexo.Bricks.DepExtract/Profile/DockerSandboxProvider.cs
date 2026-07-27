using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>
/// Options for <see cref="DockerSandboxProvider"/> (C++ profile only).
/// </summary>
public sealed class DockerSandboxProviderOptions
{
    /// <summary>Poco/evtx root with <c>common/processing.hpp</c>.</summary>
    public string? PocoContext { get; init; }

    /// <summary>Extracted parser tree.</summary>
    public string? ParserDir { get; init; }

    /// <summary>Qt shim directory.</summary>
    public string? ShimDir { get; init; }

    /// <summary>Sandbox image (default dep-extract:latest).</summary>
    public string Image { get; init; } = "dep-extract:latest";

    /// <summary>Entrypoint override (default g++).</summary>
    public string Entrypoint { get; init; } = "g++";
}

/// <summary>
/// Profile <see cref="ISandboxProvider"/>: materializes the artifact into
/// <paramref name="scratchRoot"/> and returns a <see cref="SandboxSpec"/>
/// via <see cref="GxxSandboxSpecFactory"/>.
/// </summary>
public sealed class DockerSandboxProvider : ISandboxProvider
{
    private readonly DockerSandboxProviderOptions _options;

    /// <summary>Creates the Docker sandbox provider for cpp-evtx.</summary>
    public DockerSandboxProvider(DockerSandboxProviderOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public object BuildSpec(GeneratedArtifact artifact, string scratchRoot)
    {
        if (artifact is null) throw new ArgumentNullException(nameof(artifact));
        if (string.IsNullOrWhiteSpace(scratchRoot))
            throw new ArgumentException("scratchRoot is required.", nameof(scratchRoot));

        var poco = _options.PocoContext ?? Environment.GetEnvironmentVariable("POCO_CONTEXT");
        if (string.IsNullOrWhiteSpace(poco)
            || !File.Exists(Path.Combine(poco, "common", "processing.hpp")))
        {
            throw new InvalidOperationException(
                "DockerSandboxProvider requires POCO_CONTEXT/common/processing.hpp");
        }

        poco = Path.GetFullPath(poco);
        DepExtractPathRules.EnsurePocoContextAllowed(poco);
        Directory.CreateDirectory(scratchRoot);

        File.WriteAllText(Path.Combine(scratchRoot, "adapted_reader.hpp"), artifact.Content ?? "");

        string? parserFull = null;
        if (!string.IsNullOrWhiteSpace(_options.ParserDir) && Directory.Exists(_options.ParserDir))
        {
            parserFull = Path.GetFullPath(_options.ParserDir);
            DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(parserFull, "parserDir");
            CopyParserLeaves(parserFull, scratchRoot);
        }

        // Qt shims go into the sandbox shim dir (never the operator source tree).
        var shimFull = ResolveShimDir(scratchRoot);
        var groundingForShims = artifact.Content ?? "";
        if (parserFull is not null)
            QtHeaderShims.EnsureForDirectory(groundingForShims, parserFull, shimFull);
        else
            QtHeaderShims.EnsureForSource(groundingForShims, shimFull);

        foreach (var (rel, content) in artifact.Files ?? new Dictionary<string, string>())
        {
            if (string.IsNullOrWhiteSpace(rel) || rel.Contains("..", StringComparison.Ordinal))
                continue;
            var dest = Path.Combine(scratchRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.WriteAllText(dest, content ?? "");
        }

        File.WriteAllText(Path.Combine(scratchRoot, "smoke.cpp"), AdapterCompileGate.BuildSmokeCpp());

        return GxxSandboxSpecFactory.Build(
            workDir: scratchRoot,
            pocoContext: poco,
            parserDir: parserFull,
            shimDir: shimFull,
            image: _options.Image,
            entrypoint: _options.Entrypoint);
    }

    private string ResolveShimDir(string scratchRoot)
    {
        if (!string.IsNullOrWhiteSpace(_options.ShimDir))
        {
            var configured = Path.GetFullPath(_options.ShimDir);
            Directory.CreateDirectory(configured);
            return configured;
        }

        var underScratch = Path.Combine(scratchRoot, "qt-shims");
        Directory.CreateDirectory(underScratch);
        return underScratch;
    }

    private static void CopyParserLeaves(string parserRoot, string work)
    {
        string[] exts = [".hpp", ".h", ".hh", ".hxx", ".cpp", ".cc", ".cxx", ".c"];
        foreach (var file in Directory.EnumerateFiles(parserRoot, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (!exts.Contains(ext, StringComparer.OrdinalIgnoreCase)) continue;
            var leaf = Path.GetFileName(file);
            if (leaf.Equals("adapted_reader.hpp", StringComparison.OrdinalIgnoreCase)) continue;
            if (leaf.Equals("smoke.cpp", StringComparison.OrdinalIgnoreCase)) continue;
            var rel = Path.GetRelativePath(parserRoot, file);
            if (rel.Contains("..", StringComparison.Ordinal)) continue;
            var dest = Path.Combine(work, rel);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest, overwrite: true);
            }
            catch
            {
                // best-effort
            }
        }
    }
}
