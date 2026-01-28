# Background Agents Architecture

## Overview

Optional embedded background agents that run continuously in the framework, with configurable commands, roles, hierarchies, and models. These agents are **dog fooded** - they use the framework's own agent infrastructure to manage themselves.

## Design Principles

1. **Optional**: Background agents are opt-in via configuration
2. **Embedded**: Part of the framework, not external services
3. **Configurable**: Commands, roles, hierarchies, models all configurable
4. **Dog Fooded**: Use framework's own `IAgent`, `Orchestrator`, `AgentFactory` infrastructure
5. **Background**: Run asynchronously without blocking main operations
6. **Hierarchical**: Support agent hierarchies (parent-child relationships)
7. **Data Sensitivity**: Mark and protect sensitive data from exfiltration
8. **RAG Integration**: Support Retrieval Augmented Generation for knowledge access
9. **Web Search**: Optional web search capabilities for agents

## Architecture

### Core Components

```
┌─────────────────────────────────────────────────────────────┐
│              BackgroundAgentService (BackgroundService)      │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │     BackgroundAgentRegistry                          │   │
│  │  - Manages agent lifecycle                           │   │
│  │  - Tracks agent state                                │   │
│  │  - Handles agent communication                       │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │     BackgroundAgentConfigLoader                      │   │
│  │  - Loads agent configs from file/CLI                  │   │
│  │  - Validates configurations                          │   │
│  │  - Creates AgentSpawnSpec from configs               │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │     Uses Existing Infrastructure:                     │   │
│  │  - AgentFactory (creates agents)                     │   │
│  │  - Orchestrator (coordinates agents)                 │   │
│  │  - LifecycleManager (manages lifecycle)              │   │
│  │  - AgentBus (agent communication)                    │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Configuration Model

```csharp
public class BackgroundAgentConfig
{
    public string Id { get; set; }                    // Unique agent identifier
    public string Name { get; set; }                  // Human-readable name
    public string Role { get; set; }                  // Agent role (e.g., "monitor", "analyzer", "optimizer")
    public string? ParentId { get; set; }             // Parent agent ID (for hierarchies)
    public string ModelProvider { get; set; }          // "openai", "azure", "ollama", "deterministic"
    public string? ModelName { get; set; }            // Specific model name (optional)
    public List<string> Commands { get; set; }        // Commands this agent can execute
    public Dictionary<string, object> Parameters { get; set; }  // Agent-specific parameters
    public BackgroundAgentSchedule Schedule { get; set; }       // When to run
    public bool Enabled { get; set; }                // Enable/disable this agent
    public DataSensitivityLevel MaxDataSensitivity { get; set; }  // Maximum sensitivity level agent can access
    public List<string> AllowedDataSensitivityLevels { get; set; }  // Specific sensitivity levels allowed
    public RAGConfig? RAG { get; set; }              // RAG configuration (optional)
    public WebSearchConfig? WebSearch { get; set; }  // Web search configuration (optional)
    public ExfiltrationPolicy ExfiltrationPolicy { get; set; }  // Policy for preventing data exfiltration
}

/// <summary>
/// Interface for data sensitivity levels - allows extensible, configurable sensitivity classification.
/// 
/// Framework provides primitive levels (Public, Internal, Confidential, Secret, TopSecret),
/// but users can define custom sensitivity levels with their own ordering and restrictions.
/// </summary>
public interface IDataSensitivityLevel : ITypeValue
{
    /// <summary>
    /// Numeric value for ordering (lower = less sensitive, higher = more sensitive).
    /// </summary>
    int SensitivityValue { get; }
    
    /// <summary>
    /// Whether this level allows external LLM calls.
    /// </summary>
    bool AllowsExternalLLM { get; }
    
    /// <summary>
    /// Whether this level allows web search.
    /// </summary>
    bool AllowsWebSearch { get; }
    
    /// <summary>
    /// Whether this level requires local-only processing.
    /// </summary>
    bool RequiresLocalOnly { get; }
    
    /// <summary>
    /// Whether this level allows network exports.
    /// </summary>
    bool AllowsNetworkExports { get; }
    
    /// <summary>
    /// Description of this sensitivity level.
    /// </summary>
    string Description { get; }
}

/// <summary>
/// Primitive sensitivity levels provided by the framework.
/// </summary>
public static class DataSensitivityLevels
{
    public static IDataSensitivityLevel Public { get; } = new PrimitiveSensitivityLevel(
        "Public", 0, true, true, false, true, "Public data, no restrictions");
    
    public static IDataSensitivityLevel Internal { get; } = new PrimitiveSensitivityLevel(
        "Internal", 1, true, true, false, true, "Internal use only");
    
    public static IDataSensitivityLevel Confidential { get; } = new PrimitiveSensitivityLevel(
        "Confidential", 2, false, true, false, false, "Confidential, restricted access");
    
    public static IDataSensitivityLevel Secret { get; } = new PrimitiveSensitivityLevel(
        "Secret", 3, false, false, false, false, "Secret, highly restricted");
    
    public static IDataSensitivityLevel TopSecret { get; } = new PrimitiveSensitivityLevel(
        "TopSecret", 4, false, false, true, false, "Top secret, maximum restrictions");
    
    /// <summary>
    /// Get sensitivity level by name (case-insensitive).
    /// </summary>
    public static IDataSensitivityLevel? FromName(string name)
    {
        return name?.ToLowerInvariant() switch
        {
            "public" => Public,
            "internal" => Internal,
            "confidential" => Confidential,
            "secret" => Secret,
            "topsecret" or "top-secret" => TopSecret,
            _ => null
        };
    }
    
    /// <summary>
    /// All primitive sensitivity levels in order.
    /// </summary>
    public static IReadOnlyList<IDataSensitivityLevel> All => new[]
    {
        Public, Internal, Confidential, Secret, TopSecret
    };
}

/// <summary>
/// Primitive sensitivity level implementation.
/// </summary>
private sealed record PrimitiveSensitivityLevel(
    string Value,
    int SensitivityValue,
    bool AllowsExternalLLM,
    bool AllowsWebSearch,
    bool RequiresLocalOnly,
    bool AllowsNetworkExports,
    string Description) : IDataSensitivityLevel;

public class RAGConfig
{
    public bool Enabled { get; set; }                // Enable RAG for this agent
    public string? VectorStoreProvider { get; set; }  // "in-memory", "sqlite", "postgres", "qdrant"
    public string? VectorStorePath { get; set; }      // Path/connection string for vector store
    public int MaxRetrievalResults { get; set; }      // Maximum number of results to retrieve
    public double SimilarityThreshold { get; set; }   // Minimum similarity score (0.0-1.0)
    public List<string>? KnowledgeSources { get; set; }  // Paths to knowledge sources (docs, code, etc.)
    public string MaxSourceSensitivity { get; set; }  // Max sensitivity level name of sources to index
}

public class WebSearchConfig
{
    public bool Enabled { get; set; }                // Enable web search for this agent
    public string? SearchProvider { get; set; }       // "bing", "google", "duckduckgo", "serpapi"
    public string? ApiKey { get; set; }               // API key for search provider
    public int MaxResults { get; set; }               // Maximum search results to return
    public bool FilterSensitiveContent { get; set; }  // Filter out potentially sensitive content
    public List<string>? AllowedDomains { get; set; } // Whitelist of allowed domains (optional)
    public List<string>? BlockedDomains { get; set; } // Blacklist of blocked domains (optional)
}

public class ExfiltrationPolicy
{
    public bool BlockExternalLLMs { get; set; }      // Block sending data to external LLM providers
    public bool BlockWebSearch { get; set; }          // Block web search for sensitive data
    public bool BlockNetworkExports { get; set; }     // Block network-based exports
    public bool RequireLocalOnly { get; set; }        // Require all processing to be local-only
    public List<string>? AllowedDestinations { get; set; }  // Whitelist of allowed destinations
    public string MaxAllowedLevel { get; set; }  // Maximum sensitivity level name that can be processed
}

public class BackgroundAgentSchedule
{
    public ScheduleType Type { get; set; }            // "continuous", "interval", "cron"
    public TimeSpan? Interval { get; set; }          // For interval type
    public string? CronExpression { get; set; }     // For cron type
    public TimeSpan? InitialDelay { get; set; }      // Delay before first run
}

public enum ScheduleType
{
    Continuous,  // Run continuously (think loop)
    Interval,    // Run at fixed intervals
    Cron         // Run on cron schedule
}
```

### Configuration File Format (JSON)

```json
{
  "backgroundAgents": {
    "enabled": true,
    "agents": [
      {
        "id": "health-monitor",
        "name": "Health Monitor Agent",
        "role": "monitor",
        "modelProvider": "deterministic",
        "commands": ["check-health", "report-metrics"],
        "schedule": {
          "type": "interval",
          "interval": "00:05:00"
        },
        "enabled": true,
        "maxDataSensitivity": "Internal",
        "exfiltrationPolicy": {
          "blockExternalLLMs": false,
          "blockWebSearch": true,
          "blockNetworkExports": false,
          "maxAllowedLevel": "Internal"
        },
        "customSensitivityLevels": {
          "CustomerData": {
            "sensitivityValue": 2,
            "allowsExternalLLM": false,
            "allowsWebSearch": false,
            "requiresLocalOnly": false,
            "allowsNetworkExports": false,
            "description": "Customer-specific data, GDPR protected"
          }
        }
      },
      {
        "id": "code-analyzer",
        "name": "Code Analysis Agent",
        "role": "analyzer",
        "parentId": "health-monitor",
        "modelProvider": "openai",
        "modelName": "gpt-4",
        "commands": ["analyze-code", "detect-issues"],
        "schedule": {
          "type": "cron",
          "cronExpression": "0 */6 * * *"
        },
        "enabled": true,
        "maxDataSensitivity": "Confidential",
        "parameters": {
          "analysisDepth": "thorough",
          "reportFormat": "json"
        },
        "rag": {
          "enabled": true,
          "vectorStoreProvider": "sqlite",
          "vectorStorePath": "./data/rag-store.db",
          "maxRetrievalResults": 5,
          "similarityThreshold": 0.7,
          "knowledgeSources": [
            "./docs",
            "./src"
          ],
          "maxSourceSensitivity": "Internal"
        },
        "webSearch": {
          "enabled": true,
          "searchProvider": "bing",
          "apiKey": "${BING_API_KEY}",
          "maxResults": 10,
          "filterSensitiveContent": true,
          "allowedDomains": ["github.com", "stackoverflow.com"]
        },
        "exfiltrationPolicy": {
          "blockExternalLLMs": false,
          "blockWebSearch": false,
          "blockNetworkExports": true,
          "maxAllowedLevel": "Confidential"
        }
      },
      {
        "id": "security-auditor",
        "name": "Security Auditor Agent",
        "role": "auditor",
        "modelProvider": "ollama",
        "modelName": "llama2",
        "commands": ["audit-security", "scan-vulnerabilities"],
        "schedule": {
          "type": "interval",
          "interval": "01:00:00"
        },
        "enabled": true,
        "maxDataSensitivity": "Secret",
        "exfiltrationPolicy": {
          "blockExternalLLMs": true,
          "blockWebSearch": true,
          "blockNetworkExports": true,
          "requireLocalOnly": true,
          "maxAllowedLevel": "Secret"
        }
      }
    ]
  }
}
```

## Data Sensitivity & Exfiltration Prevention

### Data Sensitivity Classification

All data processed by background agents is classified with sensitivity levels. The framework provides primitive levels but allows custom sensitivity levels to be defined.

#### Sensitivity Level Registry

```csharp
public interface IDataSensitivityRegistry
{
    /// <summary>
    /// Register a custom sensitivity level.
    /// </summary>
    void Register(IDataSensitivityLevel level);
    
    /// <summary>
    /// Get sensitivity level by name (checks both primitives and custom levels).
    /// </summary>
    IDataSensitivityLevel? GetByName(string name);
    
    /// <summary>
    /// Get all registered sensitivity levels (primitives + custom).
    /// </summary>
    IReadOnlyList<IDataSensitivityLevel> GetAll();
    
    /// <summary>
    /// Check if one level can access another (based on SensitivityValue).
    /// </summary>
    bool CanAccess(IDataSensitivityLevel agentLevel, IDataSensitivityLevel dataLevel);
}

public class DataSensitivityRegistry : IDataSensitivityRegistry
{
    private readonly ConcurrentDictionary<string, IDataSensitivityLevel> _customLevels = new();
    
    public void Register(IDataSensitivityLevel level)
    {
        _customLevels[level.Value] = level;
    }
    
    public IDataSensitivityLevel? GetByName(string name)
    {
        // Check primitives first
        var primitive = DataSensitivityLevels.FromName(name);
        if (primitive != null)
            return primitive;
        
        // Check custom levels
        return _customLevels.TryGetValue(name, out var custom) ? custom : null;
    }
    
    public IReadOnlyList<IDataSensitivityLevel> GetAll()
    {
        return DataSensitivityLevels.All
            .Concat(_customLevels.Values)
            .OrderBy(l => l.SensitivityValue)
            .ToList();
    }
    
    public bool CanAccess(IDataSensitivityLevel agentLevel, IDataSensitivityLevel dataLevel)
    {
        // Agent can only access data at or below its maximum sensitivity level
        return dataLevel.SensitivityValue <= agentLevel.SensitivityValue;
    }
}
```

#### Data Sensitivity Marker

```csharp
public interface IDataSensitivityMarker
{
    IDataSensitivityLevel GetSensitivityLevel(object data);
    void MarkSensitivity(object data, IDataSensitivityLevel level);
    void MarkSensitivity(object data, string levelName);
    bool CanAccess(IDataSensitivityLevel agentLevel, object data);
}

public class DataSensitivityMarker : IDataSensitivityMarker
{
    private readonly IDataSensitivityRegistry _registry;
    private readonly ConcurrentDictionary<object, IDataSensitivityLevel> _markings = new();
    
    public DataSensitivityMarker(IDataSensitivityRegistry registry)
    {
        _registry = registry;
    }
    
    public IDataSensitivityLevel GetSensitivityLevel(object data)
    {
        return _markings.TryGetValue(data, out var level) 
            ? level 
            : DataSensitivityLevels.Public;
    }
    
    public void MarkSensitivity(object data, IDataSensitivityLevel level)
    {
        _markings[data] = level;
    }
    
    public void MarkSensitivity(object data, string levelName)
    {
        var level = _registry.GetByName(levelName) 
            ?? throw new ArgumentException($"Unknown sensitivity level: {levelName}", nameof(levelName));
        MarkSensitivity(data, level);
    }
    
    public bool CanAccess(IDataSensitivityLevel agentLevel, object data)
    {
        var dataLevel = GetSensitivityLevel(data);
        return _registry.CanAccess(agentLevel, dataLevel);
    }
}
```

#### Custom Sensitivity Level Configuration

```csharp
public class CustomSensitivityLevel
{
    public string Name { get; set; } = string.Empty;
    public int SensitivityValue { get; set; }
    public bool AllowsExternalLLM { get; set; }
    public bool AllowsWebSearch { get; set; }
    public bool RequiresLocalOnly { get; set; }
    public bool AllowsNetworkExports { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CustomSensitivityLevelFactory
{
    private readonly IDataSensitivityRegistry _registry;
    
    public void RegisterFromConfig(Dictionary<string, CustomSensitivityLevel> config)
    {
        foreach (var (name, configLevel) in config)
        {
            var level = new ConfigurableSensitivityLevel(
                name,
                configLevel.SensitivityValue,
                configLevel.AllowsExternalLLM,
                configLevel.AllowsWebSearch,
                configLevel.RequiresLocalOnly,
                configLevel.AllowsNetworkExports,
                configLevel.Description);
            
            _registry.Register(level);
        }
    }
}

private sealed record ConfigurableSensitivityLevel(
    string Value,
    int SensitivityValue,
    bool AllowsExternalLLM,
    bool AllowsWebSearch,
    bool RequiresLocalOnly,
    bool AllowsNetworkExports,
    string Description) : IDataSensitivityLevel;
```

### Exfiltration Prevention Policy

```csharp
public class DataExfiltrationPolicy : IPolicy
{
    private readonly IDataSensitivityMarker _sensitivityMarker;
    private readonly ExfiltrationPolicy _config;
    
    public DataExfiltrationPolicy(
        IDataSensitivityMarker sensitivityMarker,
        ExfiltrationPolicy config)
    {
        _sensitivityMarker = sensitivityMarker;
        _config = config;
    }
    
    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        // Check if tool call involves external communication
        if (IsExternalTool(call))
        {
            // Extract data from tool call
            var data = ExtractDataFromToolCall(call);
            var sensitivity = _sensitivityMarker.GetSensitivityLevel(data);
            
            // Get max allowed level from registry
            var maxAllowed = _sensitivityRegistry.GetByName(_config.MaxAllowedLevel);
            if (maxAllowed == null)
            {
                reason = $"Unknown sensitivity level: {_config.MaxAllowedLevel}";
                return false;
            }
            
            // Check policy restrictions
            if (!_sensitivityRegistry.CanAccess(maxAllowed, sensitivity))
            {
                reason = $"Data sensitivity {sensitivity} exceeds allowed level {_config.MaxAllowedLevel}";
                return false;
            }
            
            if (_config.BlockExternalLLMs && IsLLMTool(call))
            {
                reason = "External LLM calls blocked by exfiltration policy";
                return false;
            }
            
            if (_config.BlockWebSearch && IsWebSearchTool(call))
            {
                reason = "Web search blocked by exfiltration policy";
                return false;
            }
            
            if (_config.RequireLocalOnly && IsNetworkTool(call))
            {
                reason = "Network tools blocked - local-only processing required";
                return false;
            }
        }
        
        reason = "OK";
        return true;
    }
    
    private bool IsExternalTool(ToolCall call)
    {
        return call.Id.StartsWith("web.") || 
               call.Id.StartsWith("llm.") || 
               call.Id.StartsWith("api.");
    }
}
```

### Integration with Policy Engine

```csharp
// In BackgroundAgentService
private PolicyEngine CreatePolicyEngine(BackgroundAgentConfig config)
{
    var policies = new List<IPolicy>();
    
    // Add data exfiltration policy
    policies.Add(new DataExfiltrationPolicy(
        _sensitivityMarker,
        config.ExfiltrationPolicy));
    
    // Add other policies (path restrictions, etc.)
    policies.Add(new OutputPathSandboxed());
    
    return new PolicyEngine(policies);
}
```

## RAG (Retrieval Augmented Generation) Integration

### RAG Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Background Agent                         │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │     RAG Tool (ITool)                                  │  │
│  │  - search-knowledge-base                              │  │
│  │  - retrieve-context                                   │  │
│  └──────────────────────────────────────────────────────┘  │
│                        │                                    │
│                        ▼                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │     RAG Service                                       │  │
│  │  - Vector store interface                             │  │
│  │  - Embedding generation                               │  │
│  │  - Similarity search                                  │  │
│  └──────────────────────────────────────────────────────┘  │
│                        │                                    │
│                        ▼                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │     Vector Store (IVectorStore)                      │  │
│  │  - In-memory, SQLite, PostgreSQL, Qdrant             │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### RAG Implementation

```csharp
public interface IVectorStore
{
    Task<bool> InitializeAsync(CancellationToken ct);
    Task IndexAsync(string id, string text, Dictionary<string, object>? metadata, CancellationToken ct);
    Task<List<VectorSearchResult>> SearchAsync(string query, int maxResults, double minScore, CancellationToken ct);
    Task<List<VectorSearchResult>> SearchBySensitivityAsync(
        string query, 
        DataSensitivityLevel maxSensitivity,
        int maxResults, 
        double minScore, 
        CancellationToken ct);
}

public class RAGService
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IDataSensitivityMarker _sensitivityMarker;
    
    public async Task<List<VectorSearchResult>> RetrieveAsync(
        string query,
        RAGConfig config,
        CancellationToken ct)
    {
        // Generate query embedding
        var queryEmbedding = await _embeddingGenerator.GenerateAsync(query, ct);
        
        // Search with sensitivity filtering
        var results = await _vectorStore.SearchBySensitivityAsync(
            query,
            config.MaxSourceSensitivity,
            config.MaxRetrievalResults,
            config.SimilarityThreshold,
            ct);
        
        return results;
    }
}

public class RAGTool : ITool
{
    private readonly RAGService _ragService;
    private readonly RAGConfig _config;
    
    public string Id => "rag.search-knowledge-base";
    
    public ToolSchema Schema => new ToolSchema(
        Id,
        "Search the knowledge base using RAG",
        @"{
          ""type"": ""object"",
          ""properties"": {
            ""query"": { ""type"": ""string"", ""description"": ""Search query"" },
            ""maxResults"": { ""type"": ""integer"", ""description"": ""Maximum results"" }
          },
          ""required"": [""query""]
        }");
    
    public async Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
    {
        var args = toolCall.ParseArgs<RAGSearchArgs>();
        var results = await _ragService.RetrieveAsync(args.Query, _config, ct);
        
        return new ToolResult(
            new ActionDelta(s.Tick, s.Tick + 1, new[] { $"Retrieved {results.Count} results from knowledge base" }),
            results);
    }
}
```

### Knowledge Base Indexing

```csharp
public class KnowledgeBaseIndexer
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IDataSensitivityMarker _sensitivityMarker;
    
    public async Task IndexDirectoryAsync(
        string directory,
        DataSensitivityLevel sensitivityLevel,
        CancellationToken ct)
    {
        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file, ct);
            
            // Mark content with sensitivity level
            _sensitivityMarker.MarkSensitivity(content, sensitivityLevel);
            
            // Chunk content for better retrieval
            var chunks = ChunkText(content, maxChunkSize: 1000);
            
            foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
            {
                await _vectorStore.IndexAsync(
                    $"{file}:{index}",
                    chunk,
                    new Dictionary<string, object>
                    {
                        ["file"] = file,
                        ["sensitivity"] = sensitivityLevel.ToString(),
                        ["chunkIndex"] = index
                    },
                    ct);
            }
        }
    }
}
```

## Web Search Integration

### Web Search Architecture

```csharp
public interface IWebSearchProvider
{
    Task<List<WebSearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken ct);
    
    Task<List<WebSearchResult>> SearchAsync(
        string query,
        WebSearchConfig config,
        CancellationToken ct);
}

public class WebSearchTool : ITool
{
    private readonly IWebSearchProvider _searchProvider;
    private readonly WebSearchConfig _config;
    private readonly IDataSensitivityMarker _sensitivityMarker;
    
    public string Id => "web.search";
    
    public ToolSchema Schema => new ToolSchema(
        Id,
        "Search the web for information",
        @"{
          ""type"": ""object"",
          ""properties"": {
            ""query"": { ""type"": ""string"", ""description"": ""Search query"" },
            ""maxResults"": { ""type"": ""integer"", ""description"": ""Maximum results"" }
          },
          ""required"": [""query""]
        }");
    
    public async Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
    {
        // Check if web search is allowed for this agent
        if (!_config.Enabled)
        {
            throw new InvalidOperationException("Web search is not enabled for this agent");
        }
        
        var args = toolCall.ParseArgs<WebSearchArgs>();
        
        // Filter query for sensitive content
        if (_config.FilterSensitiveContent)
        {
            args.Query = FilterSensitiveTerms(args.Query);
        }
        
        var results = await _searchProvider.SearchAsync(
            args.Query,
            _config,
            ct);
        
        // Filter results by domain allowlist/blocklist
        if (_config.AllowedDomains?.Any() == true)
        {
            results = results.Where(r => 
                _config.AllowedDomains.Any(domain => r.Url.Contains(domain))).ToList();
        }
        
        if (_config.BlockedDomains?.Any() == true)
        {
            results = results.Where(r => 
                !_config.BlockedDomains.Any(domain => r.Url.Contains(domain))).ToList();
        }
        
        return new ToolResult(
            new ActionDelta(s.Tick, s.Tick + 1, new[] { $"Found {results.Count} web search results" }),
            results);
    }
    
    private string FilterSensitiveTerms(string query)
    {
        // Remove or redact potentially sensitive terms
        // This is a simplified example - real implementation would be more sophisticated
        var sensitivePatterns = new[] { "password", "secret", "key", "token" };
        foreach (var pattern in sensitivePatterns)
        {
            query = System.Text.RegularExpressions.Regex.Replace(
                query, 
                pattern, 
                "[REDACTED]", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return query;
    }
}
```

### Web Search Provider Implementations

```csharp
public class BingWebSearchProvider : IWebSearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public async Task<List<WebSearchResult>> SearchAsync(
        string query,
        WebSearchConfig config,
        CancellationToken ct)
    {
        var url = $"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}&count={config.MaxResults}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
        
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(ct);
        var results = JsonSerializer.Deserialize<BingSearchResponse>(json);
        
        return results?.WebPages?.Value?.Select(r => new WebSearchResult
        {
            Title = r.Name,
            Url = r.Url,
            Snippet = r.Snippet
        }).ToList() ?? new List<WebSearchResult>();
    }
}
```

## Implementation Approach

### Phase 1: Core Infrastructure

#### 1.1 BackgroundAgentService (BackgroundService)

```csharp
public class BackgroundAgentService : BackgroundService
{
    private readonly BackgroundAgentRegistry _registry;
    private readonly BackgroundAgentConfigLoader _configLoader;
    private readonly AgentFactory _agentFactory;
    private readonly Orchestrator _orchestrator;
    private readonly ILogger<BackgroundAgentService> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Load configurations
        var configs = await _configLoader.LoadAsync(stoppingToken);
        
        // Create and register agents
        foreach (var config in configs.Where(c => c.Enabled))
        {
            var agent = await CreateAgentFromConfigAsync(config, stoppingToken);
            await _registry.RegisterAsync(agent, config, stoppingToken);
        }
        
        // Start agent execution loops
        await _registry.StartAllAsync(stoppingToken);
        
        // Keep service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
```

#### 1.2 BackgroundAgentRegistry

```csharp
public class BackgroundAgentRegistry
{
    private readonly ConcurrentDictionary<string, BackgroundAgentInstance> _agents = new();
    private readonly AgentFactory _agentFactory;
    private readonly LifecycleManager _lifecycleManager;
    private readonly IAgentBus _agentBus;
    
    public async Task RegisterAsync(
        IAgent agent, 
        BackgroundAgentConfig config, 
        CancellationToken ct)
    {
        // Register with lifecycle manager
        var container = _agentFactory.CreateContainer(agent);
        await _lifecycleManager.RegisterAgentAsync(container, ct);
        
        // Create background instance
        var instance = new BackgroundAgentInstance
        {
            Agent = agent,
            Config = config,
            State = BackgroundAgentState.Idle
        };
        
        _agents[config.Id] = instance;
    }
    
    public async Task StartAllAsync(CancellationToken ct)
    {
        // Start each agent's execution loop based on schedule
        foreach (var instance in _agents.Values)
        {
            _ = Task.Run(() => ExecuteAgentLoopAsync(instance, ct), ct);
        }
    }
    
    private async Task ExecuteAgentLoopAsync(
        BackgroundAgentInstance instance, 
        CancellationToken ct)
    {
        // Handle initial delay
        if (instance.Config.Schedule.InitialDelay.HasValue)
        {
            await Task.Delay(instance.Config.Schedule.InitialDelay.Value, ct);
        }
        
        // Execute based on schedule type
        switch (instance.Config.Schedule.Type)
        {
            case ScheduleType.Continuous:
                await ExecuteContinuousAsync(instance, ct);
                break;
            case ScheduleType.Interval:
                await ExecuteIntervalAsync(instance, ct);
                break;
            case ScheduleType.Cron:
                await ExecuteCronAsync(instance, ct);
                break;
        }
    }
}
```

#### 1.3 BackgroundAgentConfigLoader

```csharp
public class BackgroundAgentConfigLoader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackgroundAgentConfigLoader> _logger;
    
    public async Task<List<BackgroundAgentConfig>> LoadAsync(CancellationToken ct)
    {
        // Load from appsettings.json or dedicated config file
        var section = _configuration.GetSection("BackgroundAgents:Agents");
        var configs = new List<BackgroundAgentConfig>();
        
        section.Bind(configs);
        
        // Validate configurations
        foreach (var config in configs)
        {
            ValidateConfig(config);
        }
        
        return configs;
    }
    
    private void ValidateConfig(BackgroundAgentConfig config)
    {
        if (string.IsNullOrEmpty(config.Id))
            throw new InvalidOperationException("Agent ID is required");
        
        if (config.Commands == null || config.Commands.Count == 0)
            throw new InvalidOperationException($"Agent {config.Id} must have at least one command");
        
        // Validate schedule
        ValidateSchedule(config.Schedule);
        
        // Validate RAG config if present
        if (config.RAG?.Enabled == true)
        {
            if (string.IsNullOrEmpty(config.RAG.VectorStoreProvider))
                throw new InvalidOperationException($"Agent {config.Id} RAG enabled but no provider specified");
        }
        
        // Validate web search config if present
        if (config.WebSearch?.Enabled == true)
        {
            if (string.IsNullOrEmpty(config.WebSearch.SearchProvider))
                throw new InvalidOperationException($"Agent {config.Id} web search enabled but no provider specified");
        }
        
        // Validate exfiltration policy
        if (config.ExfiltrationPolicy == null)
        {
            // Get sensitivity level to determine defaults
            var sensitivityLevel = _sensitivityRegistry.GetByName(config.MaxDataSensitivity);
            if (sensitivityLevel == null)
            {
                throw new InvalidOperationException($"Unknown sensitivity level: {config.MaxDataSensitivity}");
            }
            
            // Set defaults based on sensitivity level properties
            config.ExfiltrationPolicy = new ExfiltrationPolicy
            {
                MaxAllowedLevel = config.MaxDataSensitivity,
                BlockExternalLLMs = !sensitivityLevel.AllowsExternalLLM,
                BlockWebSearch = !sensitivityLevel.AllowsWebSearch,
                RequireLocalOnly = sensitivityLevel.RequiresLocalOnly,
                BlockNetworkExports = !sensitivityLevel.AllowsNetworkExports
            };
        }
        
        // Register custom sensitivity levels if provided
        if (config.CustomSensitivityLevels != null && config.CustomSensitivityLevels.Any())
        {
            var factory = new CustomSensitivityLevelFactory(_sensitivityRegistry);
            factory.RegisterFromConfig(config.CustomSensitivityLevels);
        }
    }
}
```

### Phase 2: Agent Creation from Config

#### 2.1 Create AgentSpawnSpec from Config

```csharp
public class BackgroundAgentSpecBuilder
{
    private readonly IDataSensitivityMarker _sensitivityMarker;
    private readonly RAGService? _ragService;
    private readonly IWebSearchProvider? _webSearchProvider;
    
    public AgentSpawnSpec BuildSpec(BackgroundAgentConfig config)
    {
        // Build system prompt based on role and commands
        var systemPrompt = BuildSystemPrompt(config);
        
        // Determine dependencies (parent-child relationships)
        var dependencies = new List<string>();
        if (!string.IsNullOrEmpty(config.ParentId))
        {
            dependencies.Add(config.ParentId);
        }
        
        // Get sensitivity level to include in parameters
        var sensitivityLevel = _sensitivityRegistry.GetByName(config.MaxDataSensitivity);
        if (sensitivityLevel == null)
        {
            throw new InvalidOperationException($"Unknown sensitivity level: {config.MaxDataSensitivity}");
        }
        
        // Add RAG and web search capabilities to parameters
        var parameters = new Dictionary<string, object>(config.Parameters ?? new Dictionary<string, object>())
        {
            ["maxDataSensitivity"] = config.MaxDataSensitivity,
            ["maxDataSensitivityValue"] = sensitivityLevel.SensitivityValue,
            ["hasRAG"] = config.RAG?.Enabled == true,
            ["hasWebSearch"] = config.WebSearch?.Enabled == true
        };
        
        return new AgentSpawnSpec
        {
            AgentId = config.Id,
            AgentType = DetermineAgentType(config.Role),
            SystemPrompt = systemPrompt,
            Dependencies = dependencies,
            Parameters = parameters
        };
    }
    
    private string BuildSystemPrompt(BackgroundAgentConfig config)
    {
        var prompt = $@"You are a {config.Role} agent named {config.Name}.

Your available commands are:
{string.Join("\n", config.Commands.Select(c => $"- {c}"))}

IMPORTANT: You can only access data with sensitivity level {config.MaxDataSensitivity} (value: {sensitivityLevel.SensitivityValue}) or lower.
Any data marked as more sensitive will be automatically blocked.
Restrictions: External LLM={sensitivityLevel.AllowsExternalLLM}, Web Search={sensitivityLevel.AllowsWebSearch}, Local Only={sensitivityLevel.RequiresLocalOnly}.";
        
        if (config.RAG?.Enabled == true)
        {
            prompt += $@"

You have access to a knowledge base via RAG (Retrieval Augmented Generation).
Use the 'rag.search-knowledge-base' tool to search for relevant information before making decisions.";
        }
        
        if (config.WebSearch?.Enabled == true)
        {
            prompt += $@"

You have access to web search capabilities.
Use the 'web.search' tool to find current information from the internet.
Note: Sensitive content will be automatically filtered from search queries.";
        }
        
        prompt += @"

Execute your commands based on your role and the current system state.
Report your findings and take actions as appropriate for your role.";
        
        return prompt;
    }
    
    private string DetermineAgentType(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "monitor" => "GenericAgent",
            "analyzer" => "CodeGenerationAgent",
            "optimizer" => "GenericAgent",
            _ => "GenericAgent"
        };
    }
    
    public IToolbox BuildToolbox(BackgroundAgentConfig config)
    {
        var tools = new List<ITool>();
        
        // Add RAG tool if enabled
        if (config.RAG?.Enabled == true && _ragService != null)
        {
            tools.Add(new RAGTool(_ragService, config.RAG));
        }
        
        // Add web search tool if enabled
        if (config.WebSearch?.Enabled == true && _webSearchProvider != null)
        {
            tools.Add(new WebSearchTool(_webSearchProvider, config.WebSearch, _sensitivityMarker));
        }
        
        // Add other standard tools...
        
        return new BackgroundAgentToolbox(tools);
    }
}
```

### Phase 3: CLI Integration (Dog Fooding)

#### 3.1 CLI Commands for Background Agents

```csharp
// nexo background-agent list
public class ListBackgroundAgentsCommand : Command
{
    public override async Task<int> ExecuteAsync(InvocationContext ctx)
    {
        var registry = ctx.ServiceProvider.GetRequiredService<BackgroundAgentRegistry>();
        var agents = registry.GetAll();
        
        // Use framework's own formatting
        var console = ctx.ServiceProvider.GetRequiredService<CliConsole>();
        console.WriteLine("Background Agents:");
        foreach (var agent in agents)
        {
            console.WriteLine($"  {agent.Config.Id}: {agent.Config.Name} ({agent.State})");
        }
        
        return 0;
    }
}

// nexo background-agent add
public class AddBackgroundAgentCommand : Command
{
    public override async Task<int> ExecuteAsync(InvocationContext ctx)
    {
        // Use framework's orchestration to add agent
        var orchestrator = ctx.ServiceProvider.GetRequiredService<Orchestrator>();
        var request = $"Add background agent with configuration: {GetConfigFromArgs(ctx)}";
        
        var result = await orchestrator.OrchestrateAsync(request);
        
        // Agent is added via orchestration (dog fooding!)
        return result.Success ? 0 : 1;
    }
}

// nexo background-agent configure
public class ConfigureBackgroundAgentCommand : Command
{
    // Uses framework's own configuration system
}
```

### Phase 4: Self-Management (Advanced Dog Fooding)

#### 4.1 Meta-Agent for Managing Background Agents

```csharp
// A background agent that manages other background agents!
public class BackgroundAgentManagerAgent : BaseDomainAgent
{
    private readonly BackgroundAgentRegistry _registry;
    
    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken)
    {
        // Use framework's own agent to manage agents
        var observation = new AgentObservation(new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["registry"] = _registry.GetAll(),
            ["systemHealth"] = GetSystemHealth()
        }));
        
        var actions = await ThinkAsync(observation, GetToolbox(), GetMemory(), cancellationToken);
        
        // Execute tool calls to manage agents
        foreach (var toolCall in actions.ToolCalls)
        {
            await ExecuteManagementCommand(toolCall, cancellationToken);
        }
        
        return new { Managed = true };
    }
    
    private IToolbox GetToolbox()
    {
        // Return toolbox with agent management tools
        return new AgentManagementToolbox(_registry);
    }
}
```

#### 4.2 Agent Management Tools

```csharp
public class AgentManagementToolbox : IToolbox
{
    private readonly BackgroundAgentRegistry _registry;
    private readonly List<ITool> _tools;
    
    public AgentManagementToolbox(BackgroundAgentRegistry registry)
    {
        _registry = registry;
        _tools = new List<ITool>
        {
            new EnableAgentTool(registry),
            new DisableAgentTool(registry),
            new RestartAgentTool(registry),
            new UpdateAgentConfigTool(registry)
        };
    }
    
    public IEnumerable<ToolSchema> Schemas() => _tools.Select(t => t.Schema);
    
    public async Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
    {
        var tool = _tools.FirstOrDefault(t => t.Id == toolCall.Id);
        if (tool == null)
            throw new InvalidOperationException($"Tool {toolCall.Id} not found");
        
        return await tool.InvokeAsync(toolCall, s, ct);
    }
    
    public IAgentMemory MemoryFor(IAgent agent) => new InMemoryAgentMemory();
}
```

## Configuration File Locations

1. **appsettings.json**: Default configuration
2. **background-agents.json**: Dedicated config file (optional)
3. **CLI commands**: Runtime configuration via `nexo background-agent configure`

## Integration Points

### Service Registration

```csharp
// In Program.cs or ServiceCollectionExtensions
services.AddBackgroundAgents(options =>
{
    options.ConfigFile = "background-agents.json";
    options.Enabled = true;
});

// Registers:
// - BackgroundAgentService (BackgroundService)
// - BackgroundAgentRegistry
// - BackgroundAgentConfigLoader
// - BackgroundAgentSpecBuilder
```

### Existing Infrastructure Reuse

- **AgentFactory**: Creates agents from configs
- **Orchestrator**: Coordinates agent execution
- **LifecycleManager**: Manages agent lifecycle
- **AgentBus**: Agent-to-agent communication
- **HealthCheckService**: Monitor agent health
- **Metrics**: Track agent performance

## Example Use Cases

### 1. Health Monitor Agent

```json
{
  "id": "health-monitor",
  "role": "monitor",
  "modelProvider": "deterministic",
  "commands": ["check-health", "report-metrics"],
  "schedule": { "type": "interval", "interval": "00:05:00" }
}
```

### 2. Code Quality Agent

```json
{
  "id": "code-quality",
  "role": "analyzer",
  "modelProvider": "openai",
  "commands": ["analyze-code", "suggest-improvements"],
  "schedule": { "type": "cron", "cronExpression": "0 0 * * *" }
}
```

### 3. Performance Optimizer Agent

```json
{
  "id": "perf-optimizer",
  "role": "optimizer",
  "parentId": "health-monitor",
  "modelProvider": "ollama",
  "commands": ["analyze-performance", "optimize"],
  "schedule": { "type": "continuous" }
}
```

## Benefits of Dog Fooding

1. **Self-Management**: Framework agents manage framework agents
2. **Consistency**: Same abstractions used internally and externally
3. **Testability**: Can test agent management using framework's own test infrastructure
4. **Extensibility**: Easy to add new agent types using existing patterns
5. **Observability**: Framework's own metrics/monitoring applies to background agents

## Implementation Phases

### Phase 1: Core Infrastructure (Week 1-2)
- BackgroundAgentService
- BackgroundAgentRegistry
- BackgroundAgentConfigLoader
- Basic configuration file support
- IDataSensitivityLevel interface and primitive levels
- DataSensitivityRegistry for extensible sensitivity levels
- DataSensitivityMarker and classification system
- CustomSensitivityLevelFactory for config-based levels
- ExfiltrationPolicy implementation

### Phase 2: Agent Creation & Security (Week 3)
- BackgroundAgentSpecBuilder
- Integration with AgentFactory
- Schedule execution (interval, cron, continuous)
- Data exfiltration prevention policies
- Sensitivity-based access control

### Phase 3: RAG Integration (Week 4)
- IVectorStore interface and implementations
- RAGService with embedding generation
- RAGTool for agent access
- Knowledge base indexing
- Sensitivity-aware vector search

### Phase 4: Web Search Integration (Week 5)
- IWebSearchProvider interface
- Bing/Google/DuckDuckGo implementations
- WebSearchTool for agent access
- Domain filtering and sensitive content filtering

### Phase 5: CLI Integration (Week 6)
- `nexo background-agent list`
- `nexo background-agent add`
- `nexo background-agent configure`
- `nexo background-agent enable/disable`
- `nexo background-agent index-knowledge` (for RAG)

### Phase 6: Self-Management (Week 7)
- Meta-agent for agent management
- Agent management tools
- Self-configuration capabilities

## Testing Strategy

1. **Unit Tests**: Test each component in isolation
2. **Integration Tests**: Test agent creation and execution
3. **E2E Tests**: Test full background agent lifecycle
4. **Dog Food Tests**: Use framework's own testing infrastructure to test background agents

## Next Steps

1. Create `Nexo.BackgroundAgents` project
2. Implement core infrastructure (Phase 1)
3. Add configuration file support
4. Integrate with existing orchestration
5. Add CLI commands
6. Implement self-management (meta-agent)
