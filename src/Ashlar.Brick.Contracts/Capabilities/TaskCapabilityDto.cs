namespace Ashlar.Brick.Contracts.Capabilities;

/// <summary>Task categories a node advertises as supported for remote routing.</summary>
public enum TaskCapabilityDto
{
    /// <summary>Natural-language or structured text generation.</summary>
    TextGeneration,

    /// <summary>Source code or script generation.</summary>
    CodeGeneration,

    /// <summary>Vector embedding generation for retrieval workflows.</summary>
    Embeddings,

    /// <summary>Image or video understanding tasks.</summary>
    Vision,

    /// <summary>Multi-step reasoning or planning tasks.</summary>
    Reasoning,

    /// <summary>Labeling, categorization, or scoring tasks.</summary>
    Classification
}
