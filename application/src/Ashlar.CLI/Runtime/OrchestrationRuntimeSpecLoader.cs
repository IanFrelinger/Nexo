using System.Text.Json;
using Ashlar.Infrastructure.IO;
using Ashlar.Orchestration.Models;

namespace Ashlar.CLI.Runtime;

/// <summary>
/// CLI-layer loader for orchestration runtime spec.
/// Kept out of orchestration assembly so raw filesystem access stays at the top layer.
/// </summary>
public static class OrchestrationRuntimeSpecLoader
{
    /// <summary>Creates a new Load instance.</summary>
    public static OrchestrationRuntimeSpec Load(string? path, string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            return Deserialize(json);
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            return Deserialize(TextFile.ReadAllText(path));
        }

        return OrchestrationRuntimeSpec.Default();
    }

    private static OrchestrationRuntimeSpec Deserialize(string json)
    {
        var cfg = JsonSerializer.Deserialize<OrchestrationRuntimeSpec>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

        return cfg ?? throw new InvalidOperationException("Failed to parse orchestration runtime spec JSON");
    }
}

