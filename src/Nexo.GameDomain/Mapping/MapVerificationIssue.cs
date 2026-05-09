namespace Nexo.GameDomain.Mapping;

/// <summary>Single diagnostic from <see cref="IMapVerificationService"/>.</summary>
public sealed record MapVerificationIssue(
    MapVerificationSeverity Severity,
    string Code,
    string Message);
