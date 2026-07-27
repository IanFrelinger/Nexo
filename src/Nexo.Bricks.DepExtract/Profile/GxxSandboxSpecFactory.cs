using Nexo.Core.Application.Execution.Ports;

namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>
/// Builds the C++ compile <see cref="SandboxSpec"/> (image, mounts, g++ argv).
/// Shared by <see cref="GxxCompileValidator"/> and <see cref="DockerSandboxProvider"/>.
/// </summary>
public static class GxxSandboxSpecFactory
{
    /// <summary>
    /// Constructs a network-none sandbox spec that compiles <c>/work/smoke.cpp</c>
    /// (+ companion sources under /work) against /common and optional /parser,/shims.
    /// </summary>
    public static SandboxSpec Build(
        string workDir,
        string pocoContext,
        string? parserDir,
        string? shimDir,
        string image,
        string entrypoint,
        ResourceLimits? limits = null)
    {
        var mounts = new List<Mount>
        {
            new(ToDockerPath(Path.Combine(pocoContext, "common")), "/common", ReadOnly: true),
            new(ToDockerPath(workDir), "/work", ReadOnly: false)
        };

        if (!string.IsNullOrWhiteSpace(parserDir) && Directory.Exists(parserDir))
            mounts.Add(new Mount(ToDockerPath(parserDir), "/parser", ReadOnly: true));

        if (!string.IsNullOrWhiteSpace(shimDir) && Directory.Exists(shimDir))
            mounts.Add(new Mount(ToDockerPath(shimDir), "/shims", ReadOnly: true));

        var command = new List<string>
        {
            "-std=c++20",
            "-I/common",
            "-I/work"
        };

        if (!string.IsNullOrWhiteSpace(shimDir) && Directory.Exists(shimDir))
            command.Add("-I/shims");

        foreach (var dir in EnumerateHeaderDirs(workDir).Take(32))
        {
            var rel = Path.GetRelativePath(workDir, dir).Replace('\\', '/');
            if (rel == ".") continue;
            command.Add($"-I/work/{rel}");
        }

        if (!string.IsNullOrWhiteSpace(parserDir) && Directory.Exists(parserDir))
        {
            command.Add("-I/parser");
            foreach (var dir in EnumerateHeaderDirs(parserDir).Take(32))
            {
                var rel = Path.GetRelativePath(parserDir, dir).Replace('\\', '/');
                if (rel == ".") continue;
                command.Add($"-I/parser/{rel}");
            }
        }

        command.Add("-o");
        command.Add("/work/smoke");
        command.Add("/work/smoke.cpp");

        foreach (var cpp in Directory.EnumerateFiles(workDir, "*.cpp", SearchOption.AllDirectories)
                     .Select(p => Path.GetRelativePath(workDir, p).Replace('\\', '/'))
                     .Where(n => !n.Equals("smoke.cpp", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(n => n, StringComparer.Ordinal))
        {
            command.Add("/work/" + cpp);
        }

        foreach (var cc in Directory.EnumerateFiles(workDir, "*.cc", SearchOption.AllDirectories)
                     .Select(p => Path.GetRelativePath(workDir, p).Replace('\\', '/'))
                     .OrderBy(n => n, StringComparer.Ordinal))
        {
            command.Add("/work/" + cc);
        }

        limits ??= new ResourceLimits(
            Memory: DockerHardening.MemoryLimit,
            Pids: int.TryParse(DockerHardening.PidsLimit, out var p) ? p : 256,
            Cpus: DockerHardening.CpusLimit,
            Timeout: TimeSpan.FromSeconds(DockerHardening.RunTimeoutSeconds));

        return new SandboxSpec(
            Image: image,
            Mounts: mounts,
            Network: NetworkAccess.None,
            Command: command,
            Limits: limits,
            Entrypoint: entrypoint);
    }

    private static IEnumerable<string> EnumerateHeaderDirs(string root)
    {
        string[] exts = [".hpp", ".h", ".hh", ".hxx"];
        if (!Directory.Exists(root))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => exts.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Select(f => Path.GetDirectoryName(f)!)
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d.Length);
    }

    private static string ToDockerPath(string path) =>
        Path.GetFullPath(path).Replace('\\', '/');
}
