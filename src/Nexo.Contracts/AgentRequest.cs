namespace Nexo.Contracts;

/// <summary>Request to invoke a named agent with optional input file path.</summary>
/// <param name="AgentName">Registered agent identifier.</param>
/// <param name="InputFilePath">Optional filesystem path to input payload.</param>
public sealed record AgentRequest(string AgentName, string? InputFilePath = null);
