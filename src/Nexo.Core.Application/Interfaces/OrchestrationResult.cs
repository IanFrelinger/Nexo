namespace Nexo.Core.Application.Interfaces;

/// <summary>Outcome of an orchestrated command execution.</summary>
/// <param name="Success">True when pre-validation, command, and post-validation all succeeded.</param>
/// <param name="Message">Failure or status message when <paramref name="Success"/> is false.</param>
public sealed record OrchestrationResult(bool Success, string? Message = null);
