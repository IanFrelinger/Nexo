namespace Nexo.GameDomain.Discord;

/// <summary>
/// Extracts short video clips from playtest recordings for Discord upload.
/// Stub: will be replaced by Phase 4 implementation.
/// </summary>
public static class VideoClipper
{
    public static string? FindFfmpeg()
    {
        var paths = new[] { "/usr/bin/ffmpeg", "/usr/local/bin/ffmpeg" };
        foreach (var path in paths)
        {
            if (System.IO.File.Exists(path))
                return path;
        }
        return null;
    }

    public static Task<string?> ClipAsync(string sourceVideo, TimeSpan start, TimeSpan duration, string outputDir, CancellationToken ct = default)
    {
        return Task.FromResult<string?>(null);
    }
}
