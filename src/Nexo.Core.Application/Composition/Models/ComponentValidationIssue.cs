namespace Nexo.Core.Application.Composition.Models;

/// <summary>
/// Deterministic validation issue surfaced by registry metadata or selection checks.
/// </summary>
public sealed record ComponentValidationIssue(
    string Code,
    string ComponentId,
    string Message);
