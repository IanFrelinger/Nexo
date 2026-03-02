namespace Nexo.Hosting;

/// <summary>
/// Options for configuring the Nexo kernel when using AddNexo().
/// </summary>
public sealed class NexoHostingOptions
{
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
    /// When true, registers background agents as IHostedService (for long-running hosts).
    /// Default: false (CLI mode; agents run on-demand).
    /// </summary>
    public bool RegisterBackgroundAgentHostedService { get; set; }
}
