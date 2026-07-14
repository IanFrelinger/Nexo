namespace Nexo.Hosting.Sdk.Options;
/// <summary>
/// Options for configuring the Nexo kernel when using AddNexo().
/// </summary>
public sealed class NexoHostingOptions
{
    /// <summary>
    /// Optional dependency profile that controls which non-core modules are registered.
    /// Defaults to <see cref="NexoDeploymentProfile.Full"/> unless overridden by
    /// NEXO_DEPLOYMENT_PROFILE.
    /// </summary>
    public NexoDeploymentProfile? DeploymentProfile { get; set; }

    /// <summary>
    /// Path to the configuration file (default: ~/.nexo/config.json).
    /// </summary>
    public string? ConfigPath { get; set; }

    /// <summary>
    /// Path to the pattern store for observation/adaptation (optional).
    /// When set, enables observation context and pattern-based adaptation.
    /// </summary>
    public string? PatternStorePath { get; set; }

    /// <summary>
    /// When true, enables Trust &amp; Information Architecture (sanitization, audit).
    /// Default: from NEXO_TRUST_ENABLED env var, or false.
    /// </summary>
    public bool? TrustEnabled { get; set; }

    /// <summary>
    /// When true, registers the Microsoft.Extensions.AI governed chat pipeline
    /// (keyed <c>IChatClient</c> targets). Default: from <c>Nexo:UseMeaiPipeline</c>
    /// / <c>NEXO_USE_MEAI_PIPELINE</c>, or false (legacy <c>IProviderFactory</c> path).
    /// </summary>
    public bool? UseMeaiPipeline { get; set; }

    /// <summary>
    /// When true, registers background agents as IHostedService (for long-running hosts).
    /// Default: false (CLI mode; agents run on-demand).
    /// </summary>
    public bool RegisterBackgroundAgentHostedService { get; set; }

    /// <summary>
    /// When true, disables the observation pipeline (pattern store, event sources, ObservationPipelineService).
    /// Default: false — observation pipeline is registered by default. Set true for lightweight/CLI-only hosts.
    /// </summary>
    public bool DisableObservationPipeline { get; set; }

    /// <summary>
    /// When true, observation pipeline store errors are fail-open (logged and skipped) instead of stopping the host.
    /// Default: null (resolved from NEXO_OBSERVATION_FAIL_OPEN env var, then false).
    /// </summary>
    public bool? ObservationFailOpen { get; set; }

    /// <summary>
    /// Base URL for the Nexo API when used as a remote client (e.g. for mobile thin client).
    /// </summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// Optional API key for authentication when connecting to Nexo API.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// When true, enables adaptive load balancing (edge/server routing via NEXO_LOAD_PREFERENCE).
    /// Default: true when NEXO_LOAD_PREFERENCE is set (edge|server|auto).
    /// </summary>
    public bool? UseAdaptiveLoadBalancing { get; set; }

    /// <summary>
    /// When set, IExecutionPlatform uses RemoteExecutionPlatform delegating to this Nexo API URL.
    /// Default: from NEXO_EXECUTION_REMOTE_URL. Use when Docker is not available (e.g. mobile, CI).
    /// </summary>
    public string? ExecutionRemoteUrl { get; set; }

    /// <summary>
    /// Strict mode configuration. When enabled, the system fails fast with verbose
    /// diagnostics during development. Set <c>NEXO_STRICT_MODE=1</c> or configure
    /// individual sub-flags. Flip to permissive (disabled) for production.
    /// Default: resolved from <c>NEXO_STRICT_MODE</c> env var, or disabled.
    /// </summary>
    public StrictModeOptions StrictMode { get; set; } = new();
}
