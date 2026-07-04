namespace Nexo.Infrastructure.Testing.Agents;

/// <summary>
/// Result of agent platform compatibility check.
/// </summary>
public record AgentCompatibilityResult(
    string Platform,
    bool IsCompatible,
    List<string> Issues);
