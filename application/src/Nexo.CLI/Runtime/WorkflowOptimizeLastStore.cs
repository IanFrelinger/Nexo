using System.Text.Json;

namespace Nexo.CLI.Runtime;

/// <summary>
/// Persisted snapshot of the most recent <c>workflow optimize</c> winner for downstream tooling
/// (for example Runtime Studio agent-set sync).
/// </summary>
public sealed record WorkflowOptimizeLastPayload
{
    public DateTimeOffset WrittenAtUtc { get; init; }
    public string OptimizeRunId { get; init; } = string.Empty;
    public bool Ok { get; init; }
    public string? WinnerCandidateId { get; init; }
    public string? WinnerRunId { get; init; }
    public string? ModelProfileId { get; init; }
    public string? CompositionId { get; init; }
    public string? RequestId { get; init; }
    public IReadOnlyList<string> OllamaModels { get; init; } = Array.Empty<string>();
}

public static class WorkflowOptimizeLastStore
{
    private const string RelativePath = ".nexo/runtime/workflow_optimize_last.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string GetPath(string repoRoot)
        => Path.GetFullPath(Path.Combine(repoRoot, RelativePath));

    public static void Write(string repoRoot, WorkflowOptimizeLastPayload payload)
    {
        var path = GetPath(repoRoot);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, JsonSerializer.Serialize(payload, JsonOptions));
    }

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
