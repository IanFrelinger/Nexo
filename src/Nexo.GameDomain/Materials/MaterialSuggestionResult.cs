namespace Nexo.GameDomain.Materials;

/// <summary>
/// Output from <see cref="IMaterialIntelligenceService"/> for engine-side material setup.
/// </summary>
public sealed record MaterialSuggestionResult(
    string Summary,
    IReadOnlyList<MaterialSurfaceHint> Hints);
