using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Utility methods for AI-enhanced agents
    /// </summary>
    public abstract partial class BaseAiEnhancedAgent
    {
        /// <summary>
        /// Extracts keywords from the given text. Keywords are identified as distinct words
        /// with a length greater than 3, converted to lowercase, and stripped of certain special characters.
        /// </summary>
        /// <param name="text">The input text from which keywords will be extracted.</param>
        /// <returns>A list of distinct, lowercase keywords derived from the input text.</returns>
        protected List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            return text.Split(new[] { ' ', ',', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 3)
                .Select(word => word.ToLowerInvariant())
                .Distinct()
                .ToList();
        }
    }
}
