using System.Collections.Generic;

namespace Nexo.Shared.Interfaces
{
    /// <summary>
    /// Interface for generating semantic cache keys based on input and metadata.
    /// </summary>
    public interface ISemanticCacheKeyGenerator
    {
        /// <summary>
        /// Generates a semantic cache key from input and metadata.
        /// </summary>
        /// <param name="input">The input to generate a key for.</param>
        /// <param name="metadata">Optional metadata to include in the key generation.</param>
        /// <returns>A semantic cache key.</returns>
        string Generate(string input, IDictionary<string, object> metadata = null);
    }
} 