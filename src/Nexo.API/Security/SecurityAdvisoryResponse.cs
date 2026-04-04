namespace Nexo.API.Security;

/// <summary>
/// Non-secret advisory for operators and the Director portal UI.
/// </summary>
public sealed record SecurityAdvisoryResponse(
    string ExposureProfile,
    string Summary,
    string[] Hints,
    string? CustomAdvisory,
    bool ShowInPortal);
