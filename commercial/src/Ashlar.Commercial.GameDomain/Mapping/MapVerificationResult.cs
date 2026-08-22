namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>Aggregate outcome of map verification.</summary>
public sealed record MapVerificationResult(
    bool Passed,
    IReadOnlyList<MapVerificationIssue> Issues,
    string Summary);
