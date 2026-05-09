using Nexo.GameDomain.Aesthetics;

namespace Nexo.GameDomain.Materials;

/// <summary>
/// Optional AI-assisted material suggestions from an <see cref="AestheticPack"/> and vector parse context (no network in default impl).
/// </summary>
public interface IMaterialIntelligenceService
{
    /// <summary>
    /// Produces palette- and strategy-driven hints; <paramref name="vectorParseKind"/> is optional (<c>mvt</c>, <c>geojson</c>, <c>osm_xml</c>, etc.).
    /// </summary>
    Task<MaterialSuggestionResult> SuggestAsync(
        AestheticPack pack,
        string? vectorParseKind,
        CancellationToken cancellationToken = default);
}
