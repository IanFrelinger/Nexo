namespace Nexo.Core.Domain;

/// <summary>
/// Centralized default values for the Nexo platform. All tunable constants live here
/// so they can be referenced by both production code and configuration documentation.
/// Override any value via environment variables or <c>appsettings.json</c> — see
/// <c>docs/Configuration.md</c> for the full reference.
/// </summary>
public static class NexoDefaults
{
    // ── LLM / Provider ────────────────────────────────────────────────
    public const int LlmRetryCount = 3;
    public const double LlmTemperature = 0.2;
    public const int LlmMaxTokens = 4096;
    public const int MockDelayMs = 30;

    // ── OpenAI ────────────────────────────────────────────────────────
    public const string OpenAiDefaultModel = "gpt-4o-mini";
    public const string OpenAiDefaultBaseUrl = "https://api.openai.com/v1/chat/completions";
    public const string OpenAiDefaultVisionModel = "gpt-4o-mini";

    // ── Azure OpenAI ──────────────────────────────────────────────────
    public const string AzureOpenAiDefaultApiVersion = "2024-06-01";

    // ── Ollama ────────────────────────────────────────────────────────
    public const string OllamaDefaultBaseUrl = "http://localhost:11434";
    public const string OllamaDefaultModel = "llama3.1";
    public const string OllamaDefaultVisionModel = "richardyoung/smolvlm2-2.2b-instruct";
    public const int OllamaDefaultTimeoutSeconds = 300;

    // ── Pipeline ──────────────────────────────────────────────────────
    public const int PipelineMaxRetryAttempts = 3;
    public const int PipelineRetryDelayMs = 100;

    // ── Configuration / Analysis ──────────────────────────────────────
    public const int AnalysisMaxComplexityThreshold = 20;
    public const int ValidationTimeoutSeconds = 300;
    public const string ConfigFileName = "config.json";
    public const string ConfigDirectoryName = ".nexo";

    // ── Audit / Buffers ───────────────────────────────────────────────
    public const int SanitizationAuditMaxEntries = 10_000;
    public const int DataDecisionAuditMaxEntries = 50_000;
    public const int AgentLogMaxEntriesPerAgent = 1_000;

    // ── Embedding ─────────────────────────────────────────────────────
    public const int EmbeddingDefaultDimension = 64;

    // ── RunPod ─────────────────────────────────────────────────────────
    public const string RunPodDefaultBaseUrl = "https://api.runpod.io";
    public const string RunPodDefaultGpuTier = "NVIDIA_A4000";
    public const int RunPodDefaultTimeoutMinutes = 10;
    public const int RunPodDefaultPollingIntervalSeconds = 2;
    public const int RunPodDefaultQueueDepthThreshold = 4;
    public const string RunPodDefaultPeerCapabilityId = "generation.capability-routing";
    public const string RunPodDefaultPeerRoutingBrickId = "generation.capability-routing";
    public const int RunPodDefaultPeerRequestTimeoutSeconds = 30;
    public const int RunPodDefaultPeerDiscoveryIntervalSeconds = 10;
    public const string RunPodDefaultPeerTrustPolicy = "trusted-preferred";

    // ── Networking ─────────────────────────────────────────────────────
    public const int NetworkBusHeartbeatIntervalSeconds = 30;
    public const int NetworkBusMaxEventHistory = 10_000;
    public const int NetworkBusDefaultMaxHops = 3;

    // ── Brick Usage ────────────────────────────────────────────────────
    public const int BrickUsageTrackerMaxEntries = 10_000;
    public const int BrickUsageTrackerRollingHourWindowSeconds = 3600;

    // ── Video ──────────────────────────────────────────────────────────
    public const int VideoDefaultFps = 5;

    // ── Routing ────────────────────────────────────────────────────────
    public const int RoutingHealthCheckIntervalSeconds = 30;
}
