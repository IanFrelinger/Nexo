namespace Nexo.Core.Application.Composition.Models;

/// <summary>Deterministic validation issue surfaced by registry metadata or selection checks.</summary>
/// <param name="Code">Stable issue code for programmatic handling.</param>
/// <param name="ComponentId">Component identifier related to the issue.</param>
/// <param name="Message">Human-readable issue description.</param>
public sealed record ComponentValidationIssue(
    string Code,
    string ComponentId,
    string Message);
