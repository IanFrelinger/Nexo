namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Task capability types used for model suitability and routing.
/// </summary>
public enum TaskCapability
{
    /// <summary>Natural language text generation.</summary>
    TextGeneration,

    /// <summary>Source code generation and completion.</summary>
    CodeGeneration,

    /// <summary>Vector embedding generation for retrieval.</summary>
    Embeddings,

    /// <summary>Image understanding and multimodal vision tasks.</summary>
    Vision,

    /// <summary>Multi-step reasoning and chain-of-thought tasks.</summary>
    Reasoning,

    /// <summary>Label or category classification tasks.</summary>
    Classification
}
