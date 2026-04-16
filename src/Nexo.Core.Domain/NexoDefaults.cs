namespace Nexo.Core.Domain;

/// <summary>
/// Centralized default values for every tunable constant in the Nexo platform.
/// <para>
/// <b>Design intent:</b> All magic numbers live here so that production code
/// never hard-codes values, and configuration documentation has a single
/// source of truth for "what happens when a setting is omitted."
/// </para>
/// <para>
/// <b>Override precedence</b> (highest → lowest):
/// environment variable → <c>appsettings.json</c> / IConfiguration binding →
/// this class.  See <c>docs/Configuration.md</c> for the full reference.
/// </para>
/// <para>
/// <b>Related files:</b>
/// <c>Nexo.Hosting.NexoServiceCollectionExtensions</c> — reads many of these
/// defaults during DI registration;
/// <c>RunPodBrickConfig</c> — RunPod-specific config that falls back to
/// constants here.
/// </para>
/// </summary>
public static class NexoDefaults
{
    // ── LLM / Provider ────────────────────────────────────────────────

    /// <summary>Number of automatic retries when an LLM call fails with a transient error.
    /// Override: <c>Nexo:Llm:RetryCount</c>.  Three strikes balances reliability against
    /// latency; exponential back-off is applied by the retry policy, not this constant.</summary>
    public const int LlmRetryCount = 3;

    /// <summary>Default sampling temperature for LLM completions.
    /// Override: <c>Nexo:Llm:Temperature</c>.  0.2 keeps output mostly
    /// deterministic while still allowing minor variation for code-gen tasks.</summary>
    public const double LlmTemperature = 0.2;

    /// <summary>Maximum token budget per LLM completion.
    /// Override: <c>Nexo:Llm:MaxTokens</c>.  4 096 is sufficient for most
    /// single-turn brick responses; workflows that need more should set this
    /// per-step via orchestration runtime specs.</summary>
    public const int LlmMaxTokens = 4096;

    /// <summary>Artificial delay (ms) injected by the mock LLM provider to
    /// simulate real-world latency during testing.
    /// Override: <c>Nexo:Llm:MockDelayMs</c>.</summary>
    public const int MockDelayMs = 30;

    // ── OpenAI ────────────────────────────────────────────────────────

    /// <summary>Default OpenAI model identifier.
    /// Override: <c>NEXO_OPENAI_MODEL</c> env var or <c>Nexo:OpenAi:Model</c>.
    /// "gpt-4o-mini" chosen for cost-efficiency in default/demo scenarios.</summary>
    public const string OpenAiDefaultModel = "gpt-4o-mini";

    /// <summary>Default OpenAI chat completions endpoint.
    /// Override: <c>NEXO_OPENAI_BASE_URL</c> or <c>Nexo:OpenAi:BaseUrl</c>.</summary>
    public const string OpenAiDefaultBaseUrl = "https://api.openai.com/v1/chat/completions";

    /// <summary>Default OpenAI model used for vision (image-input) tasks.
    /// Override: <c>Nexo:OpenAi:VisionModel</c>.</summary>
    public const string OpenAiDefaultVisionModel = "gpt-4o-mini";

    // ── Azure OpenAI ──────────────────────────────────────────────────

    /// <summary>API version sent in Azure OpenAI requests.
    /// Override: <c>Nexo:AzureOpenAi:ApiVersion</c>.  Must match a
    /// GA or preview version supported by the Azure endpoint.</summary>
    public const string AzureOpenAiDefaultApiVersion = "2024-06-01";

    // ── Ollama ────────────────────────────────────────────────────────

    /// <summary>Base URL for a local Ollama instance.
    /// Override: <c>NEXO_OLLAMA_BASE_URL</c> or <c>Nexo:Ollama:BaseUrl</c>.
    /// Defaults to localhost because Ollama typically runs as a local sidecar.</summary>
    public const string OllamaDefaultBaseUrl = "http://localhost:11434";

    /// <summary>Default Ollama text model (tag form matches <c>ollama pull</c> / <c>ollama list</c>).
    /// Override: <c>NEXO_OLLAMA_MODEL</c> or <c>Nexo:Ollama:Model</c>.</summary>
    public const string OllamaDefaultModel = "llama3.1:latest";

    /// <summary>Default Ollama vision model for image-input tasks.
    /// Override: <c>Nexo:Ollama:VisionModel</c>.</summary>
    public const string OllamaDefaultVisionModel = "richardyoung/smolvlm2-2.2b-instruct";

    /// <summary>HTTP request timeout (seconds) for Ollama calls.
    /// Override: <c>Nexo:Ollama:TimeoutSeconds</c>.  Set high (300 s)
    /// because large-model first-load can be very slow.</summary>
    public const int OllamaDefaultTimeoutSeconds = 300;

    // ── Pipeline ──────────────────────────────────────────────────────

    /// <summary>Maximum retry attempts for a failed pipeline stage before the
    /// stage is marked as permanently failed.
    /// Override: <c>Nexo:Pipelines:Execution:MaxRetryAttempts</c>.
    /// See also <c>Nexo.Infrastructure.Pipelines.PipelineExecutionOptions</c>.</summary>
    public const int PipelineMaxRetryAttempts = 3;

    /// <summary>Base delay (ms) between pipeline stage retries.
    /// Override: <c>Nexo:Pipelines:Execution:RetryDelayMs</c>.
    /// Kept short (100 ms) to avoid holding pipeline threads idle.</summary>
    public const int PipelineRetryDelayMs = 100;

    // ── Configuration / Analysis ──────────────────────────────────────

    /// <summary>Cyclomatic-complexity threshold above which a method is
    /// flagged by the code-quality analysis rule.
    /// Override: <c>Nexo:Analysis:MaxComplexityThreshold</c>.</summary>
    public const int AnalysisMaxComplexityThreshold = 20;

    /// <summary>Timeout (seconds) for the validation runner.
    /// Override: <c>Nexo:Validation:TimeoutSeconds</c>.</summary>
    public const int ValidationTimeoutSeconds = 300;

    /// <summary>Name of the Nexo configuration file looked up in
    /// <see cref="ConfigDirectoryName"/>.  Not user-overridable.</summary>
    public const string ConfigFileName = "config.json";

    /// <summary>Hidden directory name where Nexo stores repo-level config.
    /// Not user-overridable; changing it would break existing repos.</summary>
    public const string ConfigDirectoryName = ".nexo";

    // ── Audit / Buffers ───────────────────────────────────────────────

    /// <summary>Maximum entries retained in the sanitization audit ring buffer.
    /// Override: <c>Nexo:Audit:SanitizationMaxEntries</c>.
    /// 10 000 entries ≈ a few MB; older entries are silently dropped.</summary>
    public const int SanitizationAuditMaxEntries = 10_000;

    /// <summary>Maximum entries in the data-decision audit log.
    /// Override: <c>Nexo:Audit:DataDecisionMaxEntries</c>.
    /// Higher than sanitization because data decisions are smaller records.</summary>
    public const int DataDecisionAuditMaxEntries = 50_000;

    /// <summary>Per-agent cap on in-memory log entries.
    /// Override: <c>Nexo:Agents:LogMaxEntriesPerAgent</c>.</summary>
    public const int AgentLogMaxEntriesPerAgent = 1_000;

    // ── Embedding ─────────────────────────────────────────────────────

    /// <summary>Dimensionality for locally-computed embeddings.
    /// Override: <c>Nexo:Embedding:Dimension</c>.  64 dimensions is a
    /// compact trade-off for the nearest-neighbour lookups used by the
    /// semantic cache; higher dimensions improve recall at memory cost.</summary>
    public const int EmbeddingDefaultDimension = 64;

    // ── RunPod ─────────────────────────────────────────────────────────

    /// <summary>RunPod API base URL.
    /// Override: <c>Nexo:RunPod:BaseUrl</c>.</summary>
    public const string RunPodDefaultBaseUrl = "https://api.runpod.io";

    /// <summary>Default GPU tier requested when launching RunPod instances.
    /// Override: <c>Nexo:RunPod:PreferredGpuTier</c>.  A4000 balances
    /// cost and VRAM for medium-sized models.</summary>
    public const string RunPodDefaultGpuTier = "NVIDIA_A4000";

    /// <summary>Maximum time (minutes) to wait for a RunPod job to complete
    /// before timing out.
    /// Override: <c>Nexo:RunPod:Timeout</c>.</summary>
    public const int RunPodDefaultTimeoutMinutes = 10;

    /// <summary>Interval (seconds) between RunPod job status polls.
    /// Override: <c>Nexo:RunPod:PollingInterval</c>.</summary>
    public const int RunPodDefaultPollingIntervalSeconds = 2;

    /// <summary>Number of queued jobs at which the router begins considering
    /// alternative execution targets (peer network or a different GPU tier).
    /// Override: <c>Nexo:RunPod:QueueDepthThreshold</c>.</summary>
    public const int RunPodDefaultQueueDepthThreshold = 4;

    /// <summary>NCR capability identifier used to discover peers that can
    /// accept routed generation jobs.
    /// Override: <c>Nexo:RunPod:PeerCapabilityId</c>.</summary>
    public const string RunPodDefaultPeerCapabilityId = "generation.capability-routing";

    /// <summary>Brick identifier dispatched to a peer when routing a job
    /// through the peer network.
    /// Override: <c>Nexo:RunPod:PeerRoutingBrickId</c>.</summary>
    public const string RunPodDefaultPeerRoutingBrickId = "generation.capability-routing";

    /// <summary>Timeout (seconds) for a single peer-to-peer generation request.
    /// Override: <c>Nexo:RunPod:PeerRequestTimeout</c>.</summary>
    public const int RunPodDefaultPeerRequestTimeoutSeconds = 30;

    /// <summary>Interval (seconds) at which the peer discovery background
    /// task refreshes the known-peers list.
    /// Override: <c>Nexo:RunPod:PeerDiscoveryInterval</c>.</summary>
    public const int RunPodDefaultPeerDiscoveryIntervalSeconds = 10;

    /// <summary>Default peer trust policy.  "trusted-preferred" means
    /// trusted peers are tried first, but untrusted peers are still allowed
    /// as a fallback.  See <c>RunPodBrickConfig.PeerTrustPolicy</c>.
    /// Override: <c>Nexo:RunPod:PeerTrustPolicy</c>.</summary>
    public const string RunPodDefaultPeerTrustPolicy = "trusted-preferred";

    // ── Networking ─────────────────────────────────────────────────────

    /// <summary>Interval (seconds) between heartbeat messages on the
    /// network event bus.
    /// Override: <c>Nexo:NetworkBus:HeartbeatIntervalSeconds</c>.</summary>
    public const int NetworkBusHeartbeatIntervalSeconds = 30;

    /// <summary>Maximum number of events retained in the in-memory event
    /// history ring buffer.
    /// Override: <c>Nexo:NetworkBus:MaxEventHistory</c>.</summary>
    public const int NetworkBusMaxEventHistory = 10_000;

    /// <summary>Maximum hop count for multi-hop event propagation.
    /// Override: <c>Nexo:NetworkBus:DefaultMaxHops</c>.  Kept low (3)
    /// to prevent broadcast storms in large meshes.</summary>
    public const int NetworkBusDefaultMaxHops = 3;

    // ── Brick Usage ────────────────────────────────────────────────────

    /// <summary>Maximum entries in the brick-usage tracker ring buffer.
    /// Override: <c>Nexo:BrickUsage:MaxEntries</c>.</summary>
    public const int BrickUsageTrackerMaxEntries = 10_000;

    /// <summary>Rolling window (seconds) used to compute hourly usage rates.
    /// Override: <c>Nexo:BrickUsage:RollingWindowSeconds</c>.  3 600 s = 1 hour.</summary>
    public const int BrickUsageTrackerRollingHourWindowSeconds = 3600;

    // ── Video ──────────────────────────────────────────────────────────

    /// <summary>Default frames-per-second for video capture/generation tasks.
    /// Override: <c>Nexo:Video:Fps</c>.  5 fps is a deliberate compromise
    /// between file size and visual smoothness for screen-recording bricks.</summary>
    public const int VideoDefaultFps = 5;

    // ── Routing ────────────────────────────────────────────────────────

    /// <summary>Interval (seconds) between health-check probes sent to
    /// registered execution targets.
    /// Override: <c>Nexo:Routing:HealthCheckIntervalSeconds</c>.</summary>
    public const int RoutingHealthCheckIntervalSeconds = 30;
}
