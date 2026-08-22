namespace GameDirector.Domain;

/// <summary>Session state snapshot value.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="AestheticSummary">Aesthetic summary.</param>
public sealed record SessionStateSnapshot(string SessionId, string? AestheticSummary);
