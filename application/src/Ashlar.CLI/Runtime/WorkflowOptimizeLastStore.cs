using System.Text.Json;

namespace Ashlar.CLI.Runtime;

/// <summary>Persists and reads the most recent <c>workflow optimize</c> winner snapshot.</summary>
public static class WorkflowOptimizeLastStore
{
    private const string RelativePath = ".ashlar/runtime/workflow_optimize_last.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Absolute path to the optimize-last JSON file under <paramref name="repoRoot"/>.</summary>
    public static string GetPath(string repoRoot)
        => Path.GetFullPath(Path.Combine(repoRoot, RelativePath));

    /// <summary>Writes <paramref name="payload"/> to the optimize-last store, creating parent directories as needed.</summary>
    public static void Write(string repoRoot, WorkflowOptimizeLastPayload payload)
    {
        var path = GetPath(repoRoot);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, JsonSerializer.Serialize(payload, JsonOptions));
    }

    /// <summary>Reads the optimize-last payload when present and valid; otherwise returns null.</summary>
    public static WorkflowOptimizeLastPayload? TryRead(string repoRoot)
    {
        var path = GetPath(repoRoot);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<WorkflowOptimizeLastPayload>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
