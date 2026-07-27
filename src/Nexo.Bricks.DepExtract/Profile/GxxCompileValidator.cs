using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>
/// C++ compile <see cref="IPostValidator{TOut}"/>: writes the artifact into an
/// <see cref="IScratchSpace"/> workdir, builds a <see cref="SandboxSpec"/> via
/// <see cref="GxxSandboxSpecFactory"/>, runs <see cref="ISandboxedCommandRunner"/>,
/// and maps the compiler log into the validator reason for the repair loop.
/// </summary>
public sealed class GxxCompileValidator : IPostValidator<GeneratedArtifact>
{
    private readonly ISandboxedCommandRunner _runner;
    private readonly IScratchSpace _scratch;
    private readonly GxxCompileValidatorOptions _options;
    private readonly ILogger<GxxCompileValidator>? _logger;

    /// <summary>Creates a g++ compile validator for the cpp-evtx profile.</summary>
    public GxxCompileValidator(
        ISandboxedCommandRunner runner,
        IScratchSpace scratch,
        GxxCompileValidatorOptions options,
        ILogger<GxxCompileValidator>? logger = null)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _scratch = scratch ?? throw new ArgumentNullException(nameof(scratch));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<(bool ok, string? reason)> ValidateAsync(
        GeneratedArtifact output,
        CancellationToken ct)
    {
        if (output is null || string.IsNullOrWhiteSpace(output.Content))
            return (false, "adapter artifact is empty");

        var poco = _options.PocoContext ?? Environment.GetEnvironmentVariable("POCO_CONTEXT");
        if (string.IsNullOrWhiteSpace(poco)
            || !File.Exists(Path.Combine(poco, "common", "processing.hpp")))
        {
            var msg = "compile validation: POCO_CONTEXT/common/processing.hpp not available";
            return _options.RequireSuccess
                ? (false, msg)
                : (true, null);
        }

        poco = Path.GetFullPath(poco);
        try
        {
            DepExtractPathRules.EnsurePocoContextAllowed(poco);
        }
        catch (Exception ex)
        {
            return _options.RequireSuccess
                ? (false, "compile validation: poco context not allowed: " + ex.Message)
                : (true, null);
        }

        string? parserFull = null;
        if (!string.IsNullOrWhiteSpace(_options.ParserDir) && Directory.Exists(_options.ParserDir))
        {
            parserFull = Path.GetFullPath(_options.ParserDir);
            try
            {
                DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(parserFull, "parserDir");
            }
            catch (Exception ex)
            {
                var msg = "compile validation: parserDir not allowed: " + ex.Message;
                return _options.RequireSuccess ? (false, msg) : (true, null);
            }
        }

        using var scratch = _scratch.CreateScratchDir("adapter-compile");
        var work = scratch.Path;

        await File.WriteAllTextAsync(
            Path.Combine(work, "adapted_reader.hpp"),
            output.Content,
            ct).ConfigureAwait(false);

        if (parserFull is not null)
            CopyParserLeaves(parserFull, work);

        foreach (var (rel, content) in output.Files ?? new Dictionary<string, string>())
        {
            if (string.IsNullOrWhiteSpace(rel) || rel.Contains("..", StringComparison.Ordinal))
                continue;
            var dest = Path.Combine(work, rel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            await File.WriteAllTextAsync(dest, content ?? "", ct).ConfigureAwait(false);
        }

        // Qt shims into sandbox shim dir only (never parser source tree).
        var shimFull = !string.IsNullOrWhiteSpace(_options.ShimDir)
            ? Path.GetFullPath(_options.ShimDir)
            : Path.Combine(work, "qt-shims");
        Directory.CreateDirectory(shimFull);
        if (parserFull is not null)
            QtHeaderShims.EnsureForDirectory(output.Content ?? "", parserFull, shimFull, _logger);
        else
            QtHeaderShims.EnsureForSource(output.Content ?? "", shimFull, _logger);

        await File.WriteAllTextAsync(
            Path.Combine(work, "smoke.cpp"),
            AdapterCompileGate.BuildSmokeCpp(),
            ct).ConfigureAwait(false);

        var spec = GxxSandboxSpecFactory.Build(
            workDir: work,
            pocoContext: poco,
            parserDir: parserFull,
            shimDir: shimFull,
            image: _options.Image,
            entrypoint: _options.Entrypoint);

        ProcessCommandResult result;
        try
        {
            result = await _runner.RunAsync(spec, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (_options.RequireSuccess)
        {
            return (false, "compile validation failed to start sandbox: " + ex.Message);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "compile validation soft-skipped: sandbox unavailable");
            return (true, null);
        }

        if (result.Succeeded)
        {
            _logger?.LogInformation("GxxCompileValidator passed (parserDir={Parser})", parserFull ?? "(none)");
            return (true, null);
        }

        var log = string.Join('\n', new[] { result.StdOut, result.StdErr }
            .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        if (string.IsNullOrWhiteSpace(log))
            log = $"compile failed with exit code {result.ExitCode}";

        _logger?.LogWarning("GxxCompileValidator failed: {Log}", log);
        return (false, log);
    }

    private static void CopyParserLeaves(string parserRoot, string work)
    {
        string[] exts = [".hpp", ".h", ".hh", ".hxx", ".cpp", ".cc", ".cxx", ".c"];
        foreach (var file in Directory.EnumerateFiles(parserRoot, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (!exts.Contains(ext, StringComparer.OrdinalIgnoreCase)) continue;
            var leaf = Path.GetFileName(file);
            if (string.IsNullOrWhiteSpace(leaf)) continue;
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
                // best-effort; -I/parser still covers nested originals
            }
        }
    }
}
