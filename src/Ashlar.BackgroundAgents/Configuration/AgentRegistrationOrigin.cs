namespace Ashlar.BackgroundAgents.Configuration;

/// <summary>
/// Provenance of a <see cref="BackgroundAgentRegistry.RegisterAsync"/> call.
/// Machine-origin registrations must carry a resolvable parent; authored registrations are trust roots.
/// </summary>
public enum AgentRegistrationOrigin
{
    /// <summary>Human-authored configuration (boot agent_set, reload from config file).</summary>
    Authored,

    /// <summary>Machine-proposed registration; requires parent resolution and policy narrowing.</summary>
    Machine
}
