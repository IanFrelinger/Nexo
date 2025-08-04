using System;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Configuration settings for AI provider and model preferences.
    /// </summary>
    public class AISettings
    {
        /// <summary>
        /// Gets or sets the preferred AI provider ID (e.g., "openai", "ollama", "azure-openai").
        /// </summary>
        public string PreferredProvider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the preferred AI model name (e.g., "gpt-4", "llama2").
        /// </summary>
        public string PreferredModel { get; set; } = string.Empty;
    }
} 