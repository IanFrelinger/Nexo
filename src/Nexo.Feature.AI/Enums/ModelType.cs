namespace Nexo.Feature.AI.Enums
{
    /// <summary>
    /// Represents the type of AI model.
    /// </summary>
    public enum ModelType
    {
        /// <summary>
        /// Text generation model.
        /// </summary>
        TextGeneration,

        /// <summary>
        /// Code generation model.
        /// </summary>
        CodeGeneration,

        /// <summary>
        /// Code analysis model.
        /// </summary>
        CodeAnalysis,

        /// <summary>
        /// Text embedding model.
        /// </summary>
        TextEmbedding,

        /// <summary>
        /// Image generation model.
        /// </summary>
        ImageGeneration,

        /// <summary>
        /// Image analysis model.
        /// </summary>
        ImageAnalysis,

        /// <summary>
        /// Multimodal model.
        /// </summary>
        Multimodal,

        /// <summary>
        /// Custom model.
        /// </summary>
        Custom
    }
} 