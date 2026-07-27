using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Syntax-checks an adapted_reader.hpp draft against Poco common/ + proprietary parser includes via Docker+g++.
/// When <paramref name="requireSuccess"/> is true, skips are treated as failures (not soft-ok).
/// </summary>
public static class AdapterCompileGate
{
    public sealed record Result(bool? Ok, string Log, bool Skipped);

    /// <param name="parserDir">Extract duplicate / proprietary tree (mounted + flattened into the smoke workdir).</param>
    /// <param name="shimDir">Optional dedicated dir of generated Qt shim headers, mounted read-only as -I/shims.</param>
    /// <param name="requireSuccess">When true, missing Docker/POCO is a hard failure (Ok=false), not a skip.</param>
    public static async Task<Result> TryCompileAsync(
        string adapterCode,
        string? pocoContext,
        string? parserDir = null,
        string? shimDir = null,
        bool requireSuccess = false,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        pocoContext ??= Environment.GetEnvironmentVariable("POCO_CONTEXT");
        if (string.IsNullOrWhiteSpace(pocoContext)
            || !File.Exists(Path.Combine(pocoContext, "common", "processing.hpp")))
        {
            var msg = "compile gate skipped: POCO_CONTEXT/common/processing.hpp not available";
            return requireSuccess
                ? new Result(false, msg.Replace("skipped:", "failed:"), Skipped: false)
                : new Result(null, msg, Skipped: true);
        }

        pocoContext = Path.GetFullPath(pocoContext);
        DepExtractPathRules.EnsurePocoContextAllowed(pocoContext);

        string? parserFull = null;
        if (!string.IsNullOrWhiteSpace(parserDir) && Directory.Exists(parserDir))
        {
            parserFull = Path.GetFullPath(parserDir);
            try
            {
                DepExtractPathRules.EnsureUnderWorkspaceRootIfConfigured(parserFull, "parserDir");
            }
            catch (Exception ex)
            {
                var msg = $"compile gate: parserDir not allowed: {ex.Message}";
                return requireSuccess
                    ? new Result(false, msg, Skipped: false)
                    : new Result(null, msg, Skipped: true);
            }
        }

        string? shimFull = null;
        if (!string.IsNullOrWhiteSpace(shimDir) && Directory.Exists(shimDir))
            shimFull = Path.GetFullPath(shimDir);

        var tmpBase = Environment.GetEnvironmentVariable("TMPDIR");
        if (string.IsNullOrWhiteSpace(tmpBase))
        {
            tmpBase = DepExtractPathRules.TryGetWorkspaceRoot() is { } ws
                ? Path.Combine(ws, ".dep-extract-out")
                : Path.GetTempPath();
        }
        var work = Path.Combine(tmpBase, "adapter-compile-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(work);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(work, "adapted_reader.hpp"), adapterCode, cancellationToken)
                .ConfigureAwait(false);

            // Flatten proprietary headers/sources into /work so scaffold #include "Foo.hpp" resolves
            // even when the duplicate tree nests files.
            if (parserFull is not null)
                CopyParserLeaves(parserFull, work);

            await File.WriteAllTextAsync(
                Path.Combine(work, "smoke.cpp"),
                BuildSmokeCpp(),
                cancellationToken).ConfigureAwait(false);

            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add("--rm");
            DockerHardening.AddRunIsolationArgs(psi);
            psi.ArgumentList.Add("--entrypoint");
            psi.ArgumentList.Add("g++");
            psi.ArgumentList.Add("-v");
            psi.ArgumentList.Add($"{ToDockerPath(pocoContext)}/common:/common:ro");
            psi.ArgumentList.Add("-v");
            psi.ArgumentList.Add($"{ToDockerPath(work)}:/work");
            if (parserFull is not null)
            {
                psi.ArgumentList.Add("-v");
                psi.ArgumentList.Add($"{ToDockerPath(parserFull)}:/parser:ro");
            }
            if (shimFull is not null)
            {
                psi.ArgumentList.Add("-v");
                psi.ArgumentList.Add($"{ToDockerPath(shimFull)}:/shims:ro");
            }
            // Full compile+link (not -fsyntax-only) so out-of-line methods in companion .cpp
            // files are required — matches the install Dockerfile.custom gate more closely.
            psi.ArgumentList.Add("dep-extract:latest");
            psi.ArgumentList.Add("-std=c++20");
            psi.ArgumentList.Add("-I/common");
            psi.ArgumentList.Add("-I/work");
            // Generated Qt shims live in a dedicated mount; -I/shims resolves both the flat
            // leaf (<QMessageBox>) and the qualified form (<QtWidgets/QMessageBox>).
            if (shimFull is not null)
                psi.ArgumentList.Add("-I/shims");
            foreach (var dir in EnumerateHeaderDirs(work).Take(32))
            {
                var rel = Path.GetRelativePath(work, dir).Replace('\\', '/');
                if (rel == ".") continue;
                psi.ArgumentList.Add($"-I/work/{rel}");
            }
            if (parserFull is not null)
            {
                psi.ArgumentList.Add("-I/parser");
                // Real projects use parent-relative includes ("../drw_base.h").
                // Structure-preserving copy into /work plus each header-bearing
                // subdirectory as -I keeps those chains resolving.
                foreach (var dir in EnumerateHeaderDirs(parserFull).Take(32))
                {
                    var rel = Path.GetRelativePath(parserFull, dir).Replace('\\', '/');
                    if (rel == ".") continue;
                    psi.ArgumentList.Add($"-I/parser/{rel}");
                }
            }
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add("/work/smoke");
            psi.ArgumentList.Add("/work/smoke.cpp");
            foreach (var cpp in Directory.EnumerateFiles(work, "*.cpp", SearchOption.AllDirectories)
                         .Select(p => Path.GetRelativePath(work, p).Replace('\\', '/'))
                         .Where(n => !n.Equals("smoke.cpp", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(n => n, StringComparer.Ordinal))
            {
                psi.ArgumentList.Add("/work/" + cpp);
            }
            foreach (var cc in Directory.EnumerateFiles(work, "*.cc", SearchOption.AllDirectories)
                         .Select(p => Path.GetRelativePath(work, p).Replace('\\', '/'))
                         .OrderBy(n => n, StringComparer.Ordinal))
            {
                psi.ArgumentList.Add("/work/" + cc);
            }

            Process? p;
            try
            {
                p = Process.Start(psi);
            }
            catch (Exception ex)
            {
                var msg = "compile gate skipped: failed to start docker: " + ex.Message;
                return requireSuccess
                    ? new Result(false, msg.Replace("skipped:", "failed:"), Skipped: false)
                    : new Result(null, msg, Skipped: true);
            }

            if (p is null)
            {
                var msg = "compile gate skipped: failed to start docker";
                return requireSuccess
                    ? new Result(false, msg.Replace("skipped:", "failed:"), Skipped: false)
                    : new Result(null, msg, Skipped: true);
            }

            using (p)
            {
                var stdoutTask = p.StandardOutput.ReadToEndAsync(cancellationToken);
                var stderrTask = p.StandardError.ReadToEndAsync(cancellationToken);
                int exitCode;
                try
                {
                    exitCode = await DockerHardening.WaitForExitWithTimeoutAsync(
                        p, DockerHardening.RunTimeoutSeconds, cancellationToken).ConfigureAwait(false);
                }
                catch (TimeoutException tex)
                {
                    return new Result(false, tex.Message, Skipped: false);
                }
                var stdout = await stdoutTask.ConfigureAwait(false);
                var stderr = await stderrTask.ConfigureAwait(false);
                var log = (stdout + "\n" + stderr).Trim();
                if (exitCode == 0)
                {
                    logger?.LogInformation("Adapter compile gate passed (parserDir={Parser})", parserFull ?? "(none)");
                    return new Result(true, string.IsNullOrWhiteSpace(log) ? "ok" : log, Skipped: false);
                }

                logger?.LogWarning("Adapter compile gate failed: {Log}", log);
                return new Result(false, log, Skipped: false);
            }
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Constructs CustomEventReader and type-checks a next() call so the seam is real, not just #includes.
    /// Runtime open failures are swallowed — missing recording must not fail the syntax gate.
    /// </summary>
    public static string BuildSmokeCpp() =>
        """
        #include "adapted_reader.hpp"
        #include <filesystem>
        int main() {
          try {
            fileproc::CustomEventReader r(std::filesystem::path("/work/smoke.recording"));
            fileproc::Event e;
            (void)r.next(e, 64);
            (void)r.position();
          } catch (...) {
            // ctor/open may throw without a real recording — still proves the type checks.
          }
          return 0;
        }
        """;

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
            // Preserve relative layout so "#include \"../util/Types.hpp\"" matches install.
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

    /// <summary>Distinct directories under the duplicate that contain C/C++ headers.</summary>
    private static IEnumerable<string> EnumerateHeaderDirs(string parserRoot)
    {
        string[] exts = [".hpp", ".h", ".hh", ".hxx"];
        return Directory.EnumerateFiles(parserRoot, "*", SearchOption.AllDirectories)
            .Where(f => exts.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Select(f => Path.GetDirectoryName(f)!)
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d.Length);
    }

    /// <summary>Docker Desktop on Windows accepts forward-slash paths.</summary>
    private static string ToDockerPath(string path) =>
        path.Replace('\\', '/');
}
