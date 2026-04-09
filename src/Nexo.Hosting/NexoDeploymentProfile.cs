namespace Nexo.Hosting;

/// <summary>
/// Deployment profiles that control which optional Nexo modules are registered.
/// </summary>
public enum NexoDeploymentProfile
{
    /// <summary>
    /// Registers the full Nexo stack (current AddNexo behavior).
    /// </summary>
    Full = 0,

    /// <summary>
    /// Registers all modules intended for long-running server hosts.
    /// Currently equivalent to <see cref="Full"/>.
    /// </summary>
    Server = 1,

    /// <summary>
    /// Registers a reduced edge profile (no background agents, observation, or runtime transport).
    /// </summary>
    Edge = 2,

    /// <summary>
    /// Registers an air-gapped profile that avoids cloud/trust plumbing and heavy host integrations.
    /// </summary>
    AirGapped = 3,

    /// <summary>
    /// Registers only core system modules and omits optional integrations.
    /// </summary>
    System = 4
}
