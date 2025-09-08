using System.Collections.Generic;
using Nexo.Shared.Interfaces;

namespace Nexo.Shared.Services
{
    /// <summary>
    /// Service implementation of ISemanticCacheKeyGenerator that wraps the static SemanticCacheKeyGenerator.
    /// </summary>
    public class SemanticCacheKeyGeneratorService : ISemanticCacheKeyGenerator
    {
        /// <summary>
        /// Generates a semantic cache key from input and metadata.
        /// </summary>
        /// <param name="input">The input to generate a key for.</param>
        /// <param name="metadata">Optional metadata to include in the key generation.</param>
        /// <returns>A semantic cache key.</returns>
        public string Generate(string input, IDictionary<string, object>? metadata = null)
        {
            return SemanticCacheKeyGenerator.Generate(input, metadata);
        }
    }
} 