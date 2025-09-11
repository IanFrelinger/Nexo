namespace Nexo.Core.Domain.Enums.AI
{
    /// <summary>
    /// Types of AI providers available
    /// </summary>
    public enum AIProviderType
    {
        Mock,
        LlamaWebAssembly,
        LlamaNative,
        OpenAI,
        Anthropic,
        AzureOpenAI,
        GoogleAI,
        HuggingFace
    }

    /// <summary>
    /// Types of AI engines
    /// </summary>
    public enum AIEngineType
    {
        Mock,
        LlamaWebAssembly,
        LlamaNative,
        OpenAI,
        Anthropic,
        AzureOpenAI,
        GoogleAI,
        HuggingFace,
        CodeLlama,
        CodeT5,
        StarCoder,
        Custom
    }

    /// <summary>
    /// Model precision levels
    /// </summary>
    public enum ModelPrecision
    {
        F16,
        F32,
        Q8_0,
        Q4_K_M,
        Q4_0,
        Q3_K_M,
        Q2_K
    }

    /// <summary>
    /// AI operation types
    /// </summary>
    public enum AIOperationType
    {
        CodeGeneration,
        CodeReview,
        CodeOptimization,
        Documentation,
        Testing,
        Refactoring,
        Analysis,
        Translation
    }

    /// <summary>
    /// AI priority levels
    /// </summary>
    public enum AIPriority
    {
        Performance,
        Quality,
        Speed,
        Accuracy,
        Balanced
    }

    /// <summary>
    /// AI provider status
    /// </summary>
    public enum AIProviderStatus
    {
        Available,
        Unavailable,
        Initializing,
        Error,
        Maintenance
    }

    /// <summary>
    /// AI operation status
    /// </summary>
    public enum AIOperationStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled,
        Timeout,
        Error
    }

    /// <summary>
    /// Model loading strategies
    /// </summary>
    public enum ModelLoadingStrategy
    {
        Lazy,
        Eager,
        Progressive,
        Streaming
    }

    /// <summary>
    /// AI response confidence levels
    /// </summary>
    public enum AIConfidenceLevel
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    /// <summary>
    /// AI resource requirements
    /// </summary>
    public enum AIResourceRequirement
    {
        Minimal,
        Low,
        Medium,
        High,
        Maximum
    }

    /// <summary>
    /// Model status
    /// </summary>
    public enum ModelStatus
    {
        Available,
        Unavailable,
        Downloading,
        Installing,
        Error,
        Maintenance
    }

    /// <summary>
    /// Model quantization types
    /// </summary>
    public enum ModelQuantization
    {
        None,
        Q8_0,
        Q4_K_M,
        Q4_0,
        Q3_K_M,
        Q2_K
    }

}
