namespace Nexo.Feature.AI.Enums;

/// <summary>
/// Types of AI models supported by the system
/// </summary>
public enum ModelType
{
    /// <summary>
    /// Text generation models (GPT, Claude, etc.)
    /// </summary>
    TextGeneration,
    
    /// <summary>
    /// Code generation models (Codex, Copilot, etc.)
    /// </summary>
    CodeGeneration,
    
    /// <summary>
    /// Chat models for conversational AI
    /// </summary>
    Chat,
    
    /// <summary>
    /// Analysis and reasoning models
    /// </summary>
    Analysis,
    
    /// <summary>
    /// Optimization models
    /// </summary>
    Optimization,
    
    /// <summary>
    /// Text embedding models
    /// </summary>
    TextEmbedding,
    
    /// <summary>
    /// Llama models
    /// </summary>
    Llama
}
