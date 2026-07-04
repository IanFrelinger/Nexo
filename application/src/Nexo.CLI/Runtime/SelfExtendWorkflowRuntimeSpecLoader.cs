using System.Text.Json;
using Nexo.Infrastructure.IO;

namespace Nexo.CLI.Runtime;

/// <summary>
/// CLI-layer loader for self-extend workflow runtime spec.
/// </summary>
public static class SelfExtendWorkflowRuntimeSpecLoader
{
    /// <summary>Creates a new Load instance.</summary>
    public static SelfExtendWorkflowRuntimeSpec Load(string? path, string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
            return Deserialize(json);

        if (!string.IsNullOrWhiteSpace(path))
            return Deserialize(TextFile.ReadAllText(path));

        return SelfExtendWorkflowRuntimeSpec.Default();
    }

    private static SelfExtendWorkflowRuntimeSpec Deserialize(string json)
    {
        var cfg = JsonSerializer.Deserialize<SelfExtendWorkflowRuntimeSpec>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

        return cfg ?? throw new InvalidOperationException("Failed to parse self-extend workflow runtime spec JSON");
    }
}
