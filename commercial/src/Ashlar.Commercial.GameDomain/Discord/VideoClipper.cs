namespace Ashlar.Commercial.GameDomain.Discord;
using System.Diagnostics;

/// <summary>
/// Wraps ffmpeg to extract highlight clips from full playtest recordings.
/// </summary>
public static class VideoClipper
{
    private static readonly string[] CommonPaths =
    [
        "/usr/bin/ffmpeg",
        "/usr/local/bin/ffmpeg",
        "/opt/homebrew/bin/ffmpeg",
        @"C:\ffmpeg\bin\ffmpeg.exe",
        @"C:\Program Files\ffmpeg\bin\ffmpeg.exe"
    ];

    /// <summary>
    /// Extracts a clip from <paramref name="inputPath"/> starting at
    /// <paramref name="startSeconds"/> for <paramref name="durationSeconds"/> and
    /// writes it to <paramref name="outputPath"/> using stream copy (no re-encode).
    /// </summary>
    public static async Task<bool> ClipAsync(
        string inputPath,
        double startSeconds,
        double durationSeconds,
        string outputPath,
        CancellationToken ct = default)
    {
        var ffmpeg = FindFfmpeg();
        if (ffmpeg is null) return false;
        if (!File.Exists(inputPath)) return false;

        var dir = Path.GetDirectoryName(outputPath);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = $"-ss {startSeconds:F3} -t {durationSeconds:F3} -i \"{inputPath}\" -c copy \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null) return false;

        await process.WaitForExitAsync(ct);
        return process.ExitCode == 0 && File.Exists(outputPath);
    }

    /// <summary>
    /// Searches PATH and common install locations for ffmpeg.
    /// Returns the full path or <c>null</c> if not found.
    /// </summary>
    public static string? FindFfmpeg()
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        var executable = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

        foreach (var dir in pathVar.Split(separator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir, executable);
            if (File.Exists(candidate))
                return candidate;
        }

        foreach (var path in CommonPaths)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    /// <summary>Returns whether ffmpeg is available on this system.</summary>
    public static bool IsAvailable() => FindFfmpeg() is not null;

    /// <summary>Returns the file size in bytes, or 0 if the file does not exist.</summary>
    public static long GetFileSizeBytes(string path) =>
        File.Exists(path) ? new FileInfo(path).Length : 0;
}
