using System.Text.Json;
using Nexo.Infrastructure.IO;

namespace Nexo.CLI.Runtime;

/// <summary>
/// CLI loader for workflow lab runtime spec files.
/// </summary>
public static class WorkflowLabRuntimeSpecLoader
{
    /// <summary>Loads a workflow lab runtime spec from inline JSON, a file path, or defaults.</summary>
    public static WorkflowLabRuntimeSpec Load(string? path, string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
            return Deserialize(json);

        if (!string.IsNullOrWhiteSpace(path))
            return Deserialize(TextFile.ReadAllText(path));

        return WorkflowLabRuntimeSpec.Default();
    }

    private static WorkflowLabRuntimeSpec Deserialize(string json)
    {
        var cfg = JsonSerializer.Deserialize<WorkflowLabRuntimeSpec>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

        return cfg ?? throw new InvalidOperationException("Failed to parse workflow lab runtime spec JSON.");
    }
}
