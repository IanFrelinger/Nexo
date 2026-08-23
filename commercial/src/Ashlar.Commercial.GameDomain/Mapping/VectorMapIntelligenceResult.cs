namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>
/// Heuristic output from <see cref="IVectorMapIntelligenceService"/>.
/// </summary>
public sealed record VectorMapIntelligenceResult(
    string Summary,
    IReadOnlyList<string> Notes);
