namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Configuration for a background agent.
/// 
/// Defines all aspects of a background agent including:
/// - Identity and role
/// - Model provider and configuration
/// - Commands and parameters
/// - Schedule (when to run)
/// - Data sensitivity restrictions
/// - RAG and web search capabilities
/// - Exfiltration prevention policies
/// </summary>
public class BackgroundAgentConfig
{
    /// <summary>
    /// Unique agent identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable agent name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Agent role (e.g., "monitor", "analyzer", "optimizer", "auditor").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Parent agent ID (for hierarchies).
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// Model provider ("openai", "azure", "ollama", "deterministic").
    /// </summary>
    public string ModelProvider { get; set; } = "deterministic";

    /// <summary>
    /// Specific model name (optional, e.g., "gpt-4", "llama2").
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Commands this agent can execute.
    /// </summary>
    public List<string> Commands { get; set; } = new();

    /// <summary>
    /// Agent-specific parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Schedule configuration (when to run).
    /// </summary>
    public BackgroundAgentSchedule Schedule { get; set; } = new();

    /// <summary>
    /// Whether this agent is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum data sensitivity level this agent can access (by name).
    /// </summary>
    public string MaxDataSensitivity { get; set; } = "Public";

    /// <summary>
    /// Specific sensitivity level names allowed (optional, for fine-grained control).
    /// </summary>
    public List<string>? AllowedDataSensitivityLevels { get; set; }

    /// <summary>
    /// Custom sensitivity levels defined for this agent (optional).
    /// </summary>
    public Dictionary<string, DataSensitivity.CustomSensitivityLevel>? CustomSensitivityLevels { get; set; }

    /// <summary>
    /// RAG (Retrieval Augmented Generation) configuration (optional).
    /// </summary>
    public RAGConfig? RAG { get; set; }

    /// <summary>
    /// Web search configuration (optional).
    /// </summary>
    public WebSearchConfig? WebSearch { get; set; }

    /// <summary>
    /// Exfiltration prevention policy.
    /// </summary>
    public ExfiltrationPolicy ExfiltrationPolicy { get; set; } = new();
}

/// <summary>
/// Schedule configuration for background agent execution.
/// </summary>
public class BackgroundAgentSchedule
{
    /// <summary>
    /// Schedule type: "continuous", "interval", or "cron".
    /// </summary>
    public ScheduleType Type { get; set; } = ScheduleType.Interval;

    /// <summary>
    /// Interval for interval type (e.g., "00:05:00" for 5 minutes).
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// Cron expression for cron type (e.g., "0 */6 * * *" for every 6 hours).
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Initial delay before first execution.
    /// </summary>
    public TimeSpan? InitialDelay { get; set; }
}

/// <summary>
/// Schedule type enumeration.
/// </summary>
public enum ScheduleType
{
    /// <summary>
    /// Run continuously (think loop).
    /// </summary>
    Continuous,

    /// <summary>
    /// Run at fixed intervals.
    /// </summary>
    Interval,

    /// <summary>
    /// Run on cron schedule.
    /// </summary>
    Cron
}

/// <summary>
/// RAG (Retrieval Augmented Generation) configuration.
/// </summary>
public class RAGConfig
{
    /// <summary>
    /// Whether RAG is enabled for this agent.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Vector store provider ("in-memory", "sqlite", "postgres", "qdrant").
    /// </summary>
    public string? VectorStoreProvider { get; set; }

    /// <summary>
    /// Vector store path or connection string.
    /// </summary>
    public string? VectorStorePath { get; set; }

    /// <summary>
    /// Maximum number of results to retrieve.
    /// </summary>
    public int MaxRetrievalResults { get; set; } = 5;

    /// <summary>
    /// Minimum similarity score (0.0-1.0).
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.7;

    /// <summary>
    /// Paths to knowledge sources (directories or files).
    /// </summary>
    public List<string>? KnowledgeSources { get; set; }

    /// <summary>
    /// Maximum sensitivity level name of sources to index.
    /// </summary>
    public string MaxSourceSensitivity { get; set; } = "Internal";
}

/// <summary>
/// Web search configuration.
/// </summary>
public class WebSearchConfig
{
    /// <summary>
    /// Whether web search is enabled for this agent.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Search provider ("bing", "google", "duckduckgo", "serpapi").
    /// </summary>
    public string? SearchProvider { get; set; }

    /// <summary>
    /// API key for search provider.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Maximum search results to return.
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Whether to filter potentially sensitive content from queries.
    /// </summary>
    public bool FilterSensitiveContent { get; set; } = true;

    /// <summary>
    /// Whitelist of allowed domains (optional).
    /// </summary>
    public List<string>? AllowedDomains { get; set; }

    /// <summary>
    /// Blacklist of blocked domains (optional).
    /// </summary>
    public List<string>? BlockedDomains { get; set; }
}

/// <summary>
/// Exfiltration prevention policy.
/// </summary>
public class ExfiltrationPolicy
{
    /// <summary>
    /// Whether to block sending data to external LLM providers.
    /// </summary>
    public bool BlockExternalLLMs { get; set; }

    /// <summary>
    /// Whether to block web search for sensitive data.
    /// </summary>
    public bool BlockWebSearch { get; set; }

    /// <summary>
    /// Whether to block network-based exports.
    /// </summary>
    public bool BlockNetworkExports { get; set; }

    /// <summary>
    /// Whether to require all processing to be local-only.
    /// </summary>
    public bool RequireLocalOnly { get; set; }

    /// <summary>
    /// Whitelist of allowed destinations (optional).
    /// </summary>
    public List<string>? AllowedDestinations { get; set; }

    /// <summary>
    /// Maximum sensitivity level name that can be processed.
    /// </summary>
    public string MaxAllowedLevel { get; set; } = "Public";
}
