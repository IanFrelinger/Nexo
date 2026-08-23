namespace Ashlar.Core.Application.Environments;

/// <summary>Input for material / texture intelligence.</summary>
/// <param name="StylePreset">High-level style (e.g. mediterranean_facade, industrial_roof).</param>
/// <param name="Tags">Optional tags from OSM or design doc (building:levels, surface=asphalt).</param>
/// <param name="Context">Correlation and hints.</param>
/// <param name="MaxMaterials">Cap on returned suggestions.</param>
public sealed record MaterialIntelligenceRequest(
    string StylePreset,
    IReadOnlyDictionary<string, string> Tags,
    MapDataRequestContext Context,
    int MaxMaterials = 16);
