using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ashlar.CLI.Runtime;

/// <summary>Adaptive runtime execution history store.</summary>
public static class AdaptiveRuntimeExecutionHistoryStore
{
    private const string RelativePath = ".ashlar/runtime/runtime_execute_history.jsonl";

    /// <summary>Resolves the adaptive runtime history file path under the repository root.</summary>
    public static string GetPath(string repoRoot)
        => Path.GetFullPath(Path.Combine(repoRoot, RelativePath));

    /// <summary>Reads the most recent execution reports ordered by start time descending.</summary>
    public static IReadOnlyList<AdaptiveRuntimeExecutionReport> ReadRecent(string repoRoot, int maxItems = 200)
    {
        if (maxItems <= 0)
            return Array.Empty<AdaptiveRuntimeExecutionReport>();

        var path = GetPath(repoRoot);
        if (!File.Exists(path))
            return Array.Empty<AdaptiveRuntimeExecutionReport>();

        var parsed = new List<AdaptiveRuntimeExecutionReport>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            try
            {
                var item = JsonSerializer.Deserialize<AdaptiveRuntimeExecutionReport>(line);
                if (item != null)
                    parsed.Add(item);
            }
            catch
            {
                // Ignore malformed lines to keep store resilient.
            }
        }

        return parsed
            .OrderByDescending(p => p.StartedAtUtc)
            .Take(maxItems)
            .ToArray();
    }

    /// <summary>Appends an execution report to the adaptive runtime JSONL store.</summary>
    public static void Append(string repoRoot, AdaptiveRuntimeExecutionReport report)
    {
        var path = GetPath(repoRoot);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var line = JsonSerializer.Serialize(report);
        File.AppendAllText(path, line + Environment.NewLine);
    }
}
