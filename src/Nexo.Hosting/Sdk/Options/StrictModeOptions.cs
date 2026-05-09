namespace Nexo.Hosting;

/// <summary>
/// Controls Nexo strict mode behavior. When enabled, the system fails fast with
/// verbose diagnostics — ideal for development and CI. Flip to permissive once
/// confident in the agentic layer for production deployments.
///
/// Environment variable: <c>NEXO_STRICT_MODE</c> (<c>1</c> / <c>true</c> to enable).
/// </summary>
public sealed class StrictModeOptions
{
    /// <summary>
    /// Configuration section name for binding from <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "Nexo:StrictMode";

    /// <summary>
    /// Master switch. When <c>true</c>, all strict-mode sub-flags default to their
    /// fail-fast values unless explicitly overridden.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Throw on the first validation failure instead of collecting all failures.
    /// Default: matches <see cref="Enabled"/>.
    /// </summary>
    public bool? FailFastOnValidationErrors { get; set; }

    /// <summary>
    /// Throw when a provider is misconfigured rather than falling back silently.
    /// Default: matches <see cref="Enabled"/>.
    /// </summary>
    public bool? FailFastOnProviderErrors { get; set; }

    /// <summary>
    /// Throw when pipeline stages encounter transient errors instead of retrying.
    /// Default: matches <see cref="Enabled"/>.
    /// </summary>
    public bool? FailFastOnPipelineErrors { get; set; }

    /// <summary>
    /// Emit verbose diagnostics (debug-level logging, detailed error messages).
    /// Default: matches <see cref="Enabled"/>.
    /// </summary>
    public bool? VerboseDiagnostics { get; set; }

    /// <summary>
    /// Treat configuration warnings (e.g. missing optional config, fallback to defaults)
    /// as hard errors. Default: matches <see cref="Enabled"/>.
    /// </summary>
    public bool? FailOnConfigurationWarnings { get; set; }

    // ── Resolved properties ──────────────────────────────────────────

    /// <summary>Resolved value: fail fast on validation errors.</summary>
    public bool ShouldFailFastOnValidation => FailFastOnValidationErrors ?? Enabled;

    /// <summary>Resolved value: fail fast on provider errors.</summary>
    public bool ShouldFailFastOnProvider => FailFastOnProviderErrors ?? Enabled;

    /// <summary>Resolved value: fail fast on pipeline errors.</summary>
    public bool ShouldFailFastOnPipeline => FailFastOnPipelineErrors ?? Enabled;

    /// <summary>Resolved value: emit verbose diagnostics.</summary>
    public bool ShouldEmitVerboseDiagnostics => VerboseDiagnostics ?? Enabled;

    /// <summary>Resolved value: treat config warnings as errors.</summary>
    public bool ShouldFailOnConfigurationWarnings => FailOnConfigurationWarnings ?? Enabled;
}
