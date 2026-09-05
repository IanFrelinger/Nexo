using System.Text.Json;
using Ashlar.Infrastructure.IO;

namespace Ashlar.CLI.Runtime;

/// <summary>Adaptive runtime manifest loader.</summary>
public static class AdaptiveRuntimeManifestLoader
{
    /// <summary>Creates a new Load instance.</summary>
    public static AdaptiveRuntimeManifest Load(string? path, string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
            return Normalize(Deserialize(json));

        if (!string.IsNullOrWhiteSpace(path))
            return Normalize(Deserialize(TextFile.ReadAllText(path)));

        return AdaptiveRuntimeManifest.Default();
    }

    private static AdaptiveRuntimeManifest Deserialize(string json)
    {
        var cfg = JsonSerializer.Deserialize<AdaptiveRuntimeManifest>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

        return cfg ?? throw new InvalidOperationException("Failed to parse adaptive runtime manifest JSON");
    }

    private static AdaptiveRuntimeManifest Normalize(AdaptiveRuntimeManifest source)
    {
        return source with
        {
            DomainPacks = NormalizeArray(source.DomainPacks),
            UiCapabilities = NormalizeArray(source.UiCapabilities),
            QaPolicyProfile = NormalizeQaPolicyProfile(source.QaPolicyProfile)
        };
    }

    private static string? NormalizeQaPolicyProfile(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized is not ("auto" or "demo" or "release" or "prod" or "research"))
            throw new InvalidOperationException("Invalid qaPolicyProfile. Use auto, demo, release, prod, or research.");

        return normalized;
    }

    private static string[] NormalizeArray(string[]? items)
    {
        return (items ?? Array.Empty<string>())
            .Select(i => (i ?? string.Empty).Trim())
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
