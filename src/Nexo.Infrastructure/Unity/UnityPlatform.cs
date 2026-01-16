using System.Diagnostics;
using System.Text.Json;

namespace Nexo.Infrastructure.Unity;

/// <summary>
/// Infrastructure-only helpers for Unity Editor automation (process + filesystem).
/// Keeps raw Process/File/Directory usage out of CLI/agents.
/// </summary>
public static class UnityPlatform
{
    public static string? FindUnityExecutable(string? unityBin)
    {
        var env = Environment.GetEnvironmentVariable("UNITY_BIN");
        var explicitPath = (unityBin ?? env)?.Trim();
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
        {
            return explicitPath;
        }

        // macOS default install paths (best-effort)
        var mac1 = "/Applications/Unity/Hub/Editor";
        if (Directory.Exists(mac1))
        {
            var latest = FindLatestUnityInHub(mac1);
            if (latest != null) return latest;
        }

        return null;
    }

    private static string? FindLatestUnityInHub(string hubEditorDir)
    {
        try
        {
            var dir = new DirectoryInfo(hubEditorDir);
            if (!dir.Exists) return null;

            var candidates = dir.GetDirectories()
                .OrderByDescending(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .Select(d => Path.Combine(d.FullName, "Unity.app", "Contents", "MacOS", "Unity"))
                .ToList();

            foreach (var c in candidates)
            {
                if (File.Exists(c)) return c;
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    public static async Task<int> RunUnityAsync(
        string unityPath,
        string arguments,
        bool useShellExecute,
        bool redirectOutput,
        string? workingDirectory,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = unityPath,
            Arguments = arguments,
            UseShellExecute = useShellExecute,
            CreateNoWindow = !useShellExecute,
            WorkingDirectory = workingDirectory ?? ""
        };

        if (redirectOutput)
        {
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
        }

        using var p = new Process { StartInfo = psi };
        p.Start();
        await p.WaitForExitAsync(ct);
        return p.ExitCode;
    }

    public static async Task<string> AnalyzeLogAsync(string logFilePath, CancellationToken ct)
    {
        // Minimal, deterministic JSON analysis: count error/warn lines.
        var lines = await File.ReadAllLinesAsync(logFilePath, ct);
        var errors = lines.Count(l => l.Contains("error", StringComparison.OrdinalIgnoreCase));
        var warnings = lines.Count(l => l.Contains("warning", StringComparison.OrdinalIgnoreCase));
        return JsonSerializer.Serialize(new { logFile = logFilePath, errors, warnings }, new JsonSerializerOptions { WriteIndented = true });
    }
}

