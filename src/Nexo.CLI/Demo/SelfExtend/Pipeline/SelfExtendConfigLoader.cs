using System.Text.Json;
using Nexo.Infrastructure.IO;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline;

internal static class SelfExtendConfigLoader
{
    public static SelfExtendRuntimeConfig Load(string? filePath, string? json)
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

        return SelfExtendRuntimeConfig.Default();
    }

    private static SelfExtendRuntimeConfig Deserialize(string json)
    {
        var cfg = JsonSerializer.Deserialize<SelfExtendRuntimeConfig>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

        return cfg ?? throw new InvalidOperationException("Failed to parse self-extend spec JSON");
    }
}

