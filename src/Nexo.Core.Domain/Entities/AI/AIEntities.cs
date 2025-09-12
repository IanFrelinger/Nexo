using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums.Code;
using Nexo.Core.Domain.Enums.Safety;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Entities.AI
{
    /// <summary>
    /// Information about an AI engine
    /// </summary>
    public class AIEngineInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public AIEngineType EngineType { get; set; }
        public AIEngineType Type { get; set; }
        public string Version { get; set; } = "";
        public AIProviderType ProviderType { get; set; }
        public bool IsOfflineCapable { get; set; }
        public bool IsInitialized { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string ModelPath { get; set; } = "";
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
        public List<Nexo.Core.Domain.Enums.PlatformType> SupportedPlatforms { get; set; } = new();
        public Dictionary<string, object> Configuration { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Capabilities { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Information about an AI model
    /// </summary>
    public class ModelInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ModelId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Version { get; set; } = "";
        public AIEngineType EngineType { get; set; }
        public ModelPrecision Precision { get; set; }
        public long Size { get; set; }
        public long SizeBytes { get; set; }
        public string Format { get; set; } = "";
        public ModelQuantization Quantization { get; set; }
        public ModelStatus Status { get; set; }
        public string LocalPath { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Checksum { get; set; } = "";
        public List<Nexo.Core.Domain.Enums.PlatformType> Platform { get; set; } = new();
        public List<Nexo.Core.Domain.Enums.PlatformType> SupportedPlatforms { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsed { get; set; }
        public bool IsCached { get; set; }
        
        // Additional properties needed by the code
        public Dictionary<string, object> Capabilities { get; set; } = new();
        public string ModelType { get; set; } = "TextGeneration"; // String representation of model type
        public bool IsAvailable { get; set; } = true;
        public string DisplayName { get; set; } = "";
        public int MaxContextLength { get; set; } = 4096;
    }

    /// <summary>
    /// AI provider capabilities
    /// </summary>
    public class AIProviderCapabilities
    {
        public AIProviderType ProviderType { get; set; }
        public List<Nexo.Core.Domain.Enums.PlatformType> SupportedPlatforms { get; set; } = new();
        public List<AIOperationType> SupportedOperations { get; set; } = new();
        public AIResourceRequirement MinResourceRequirement { get; set; }
        public AIResourceRequirement MaxResourceRequirement { get; set; }
        public bool SupportsOfflineMode { get; set; }
        public bool SupportsStreaming { get; set; }
        public bool SupportsBatchProcessing { get; set; }
        public int MaxConcurrentOperations { get; set; }
        public TimeSpan MaxOperationTimeout { get; set; }
        public Dictionary<string, object> CustomCapabilities { get; set; } = new();
    }

    /// <summary>
    /// Context for AI operations
    /// </summary>
    public class AIOperationContext
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public AIOperationType OperationType { get; set; }
        public Nexo.Core.Domain.Enums.PlatformType Platform { get; set; }
        public Nexo.Core.Domain.Enums.PlatformType TargetPlatform { get; set; }
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
        public string Priority { get; set; } = "Normal";
        public string ModelName { get; set; } = "";
        public AIRequirements Requirements { get; set; } = new();
        public AIResources Resources { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI operation requirements
    /// </summary>
    public class AIRequirements
    {
        public AIPriority Priority { get; set; } = AIPriority.Balanced;
        public SafetyLevel SafetyLevel { get; set; } = SafetyLevel.Standard;
        public bool RequiresOffline { get; set; }
        public bool RequiresOfflineMode { get; set; }
        public bool RequiresHighQuality { get; set; }
        public bool RequiresFastResponse { get; set; }
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
        public double TopP { get; set; } = 0.9;
        public List<string> RequiredCapabilities { get; set; } = new();
        public Dictionary<string, object> CustomRequirements { get; set; } = new();
    }

    /// <summary>
    /// Available AI resources
    /// </summary>
    public class AIResources
    {
        public long AvailableMemory { get; set; }
        public int CpuCores { get; set; }
        public bool HasGpu { get; set; }
        public long GpuMemory { get; set; }
        public bool HasInternetConnection { get; set; }
        public long AvailableStorage { get; set; }
        public Dictionary<string, object> PlatformSpecific { get; set; } = new();
    }

    /// <summary>
    /// AI operation response
    /// </summary>
    public class AIResponse
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OperationId { get; set; } = "";
        public AIOperationType OperationType { get; set; }
        public string Content { get; set; } = "";
        public AIConfidenceLevel Confidence { get; set; }
        public double ConfidenceScore { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public int TokensGenerated { get; set; }
        public int TokensProcessed { get; set; }
        public AIOperationStatus Status { get; set; }
        public string? Error { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI code generation request
    /// </summary>
    public class CodeGenerationRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Prompt { get; set; } = "";
        public string Context { get; set; } = "";
        public string Language { get; set; } = "csharp";
        public string Framework { get; set; } = "";
        public string Code { get; set; } = "";
        public AIRequirements Requirements { get; set; } = new();
        public Dictionary<string, object> Options { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Properties accessed by application layer
        public string GeneratedCode { get; set; } = "";
        public string Explanation { get; set; } = "";
        public AIConfidenceLevel Confidence { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> Suggestions { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string? Error { get; set; }
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
    }

    /// <summary>
    /// AI code generation result
    /// </summary>
    public class CodeGenerationResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RequestId { get; set; } = "";
        public string GeneratedCode { get; set; } = "";
        public string Explanation { get; set; } = "";
        public AIConfidenceLevel Confidence { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> Suggestions { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }


    /// <summary>
    /// Code issue found during review
    /// </summary>
    public class CodeIssue
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public string Severity { get; set; } = "";
        public int Line { get; set; }
        public int LineNumber { get; set; }
        public int Column { get; set; }
        public int ColumnNumber { get; set; }
        public string FilePath { get; set; } = "";
        public string? Fix { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
    }

    /// <summary>
    /// Code suggestion from AI
    /// </summary>
    public class CodeSuggestion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public string Code { get; set; } = "";
        public string Reason { get; set; } = "";
        public AIConfidenceLevel Confidence { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Model variant for different platforms
    /// </summary>
    public class ModelVariant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ModelId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public long Size { get; set; }
        public long SizeBytes { get; set; }
        public Nexo.Core.Domain.Enums.PlatformType Platform { get; set; }
        public ModelPrecision Precision { get; set; }
        public ModelStatus Status { get; set; }
        public ModelQuantization Quantization { get; set; }
        public string FileName { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string Checksum { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI code optimization result
    /// </summary>
    public class CodeOptimizationResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OptimizedCode { get; set; } = "";
        public double OptimizationScore { get; set; }
        public List<string> Improvements { get; set; } = new();
        public double PerformanceGain { get; set; }
        public TimeSpan OptimizationTime { get; set; }
        public AIEngineType EngineType { get; set; }
        public string OriginalCode { get; set; } = "";
        public Dictionary<string, object> Metrics { get; set; } = new();
        public DateTime OptimizedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI operation configuration
    /// </summary>
    public class AIOperationConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public AIOperationType OperationType { get; set; }
        public AIProviderType PreferredProvider { get; set; }
        public List<AIProviderType> FallbackProviders { get; set; } = new();
        public AIRequirements DefaultRequirements { get; set; } = new();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
        public int MaxRetries { get; set; } = 3;
        public Dictionary<string, object> Settings { get; set; } = new();
    }
}
