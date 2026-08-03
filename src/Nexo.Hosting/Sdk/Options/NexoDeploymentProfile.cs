// Namespace is deliberately Nexo.Hosting, not the folder path: this is the type
// of NexoHostingOptions.DeploymentProfile, so it must be nameable by anyone who
// can name NexoHostingOptions. See the note in NexoHostingOptions.cs.
namespace Nexo.Hosting;
/// <summary>
/// Deployment profiles that control which optional Nexo modules are registered
/// into the DI container. Each profile maps to a <c>ModuleSelection</c> record
/// inside <see cref="NexoServiceCollectionExtensions"/> that toggles 11 boolean
/// module flags. Profiles are resolved via
/// <c>NexoHostingOptions.DeploymentProfile</c> or the <c>NEXO_DEPLOYMENT_PROFILE</c>
/// environment variable, defaulting to <see cref="Full"/>.
/// </summary>
public enum NexoDeploymentProfile
{
    /// <summary>
    /// Registers every module: node capability runtime, gRPC transport,
    /// persistence, adaptation, pipeline composition, background agents (+ RAG),
    /// observation pipeline, trust services, workflow integrations, and testing
    /// adapters. Use for development or a fully-featured server deployment.
    /// </summary>
    Full = 0,

    /// <summary>
    /// Currently identical to <see cref="Full"/> — all modules are included.
    /// Exists as a semantic alias for long-running server hosts, allowing the
    /// <c>Full</c> profile to diverge later (e.g. adding dev-only tooling)
    /// without breaking server deployments.
    /// </summary>
    Server = 1,

    /// <summary>
    /// Lightweight profile for resource-constrained or edge devices.
    /// <b>Includes:</b> persistence, pipeline composition.
    /// <b>Excludes:</b> node capability runtime, runtime transport, adaptation,
    /// background agents, background-agent RAG, observation pipeline, trust
    /// services, workflow integrations, testing adapters.
    /// </summary>
    Edge = 2,

    /// <summary>
    /// Profile for environments with no external network access.
    /// <b>Includes:</b> node capability runtime, persistence, adaptation,
    /// pipeline composition.
    /// <b>Excludes:</b> runtime transport (no gRPC egress), background agents,
    /// background-agent RAG, observation pipeline, trust services (no remote
    /// attestation), workflow integrations, testing adapters.
    /// </summary>
    AirGapped = 3,

    /// <summary>
    /// Bare-minimum profile: only core system/kernel services, no optional
    /// modules at all. Every module flag is <c>false</c>. Useful for CLI
    /// tooling, unit tests, or hosts that register integrations manually.
    /// </summary>
    System = 4
}
