using System.Text.Json;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Infrastructure.IO;

namespace Nexo.CLI.Runtime;

/// <summary>
/// CLI-layer loader for UniversalTester runtime configuration.
/// Kept out of the agent assembly so raw filesystem access stays at the top layer.
/// </summary>
public static class UniversalTesterRuntimeConfigLoader
{
    public static UniversalTesterRuntimeConfig Load(string? filePath, string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            return Deserialize(json);
        }

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var text = TextFile.ReadAllText(filePath);
            return Deserialize(text);
        }

        return UniversalTesterRuntimeConfig.Default();
    }

    private static UniversalTesterRuntimeConfig Deserialize(string json)
    {
        var cfg = JsonSerializer.Deserialize<UniversalTesterRuntimeConfig>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

        return cfg ?? throw new InvalidOperationException("Failed to parse universal tester runtime config JSON");
    }
}

