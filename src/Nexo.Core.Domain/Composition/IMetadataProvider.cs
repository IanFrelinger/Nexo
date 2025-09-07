using System.Collections.Generic;

namespace Nexo.Core.Domain.Composition
{
    /// <summary>
    /// Interface for objects that can store and retrieve metadata as key-value pairs.
    /// This provides consistent metadata handling across the domain model.
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Gets all metadata associated with this object.
        /// </summary>
        /// <returns>A dictionary containing all metadata key-value pairs</returns>
        IDictionary<string, object> GetMetadata();
        
        /// <summary>
        /// Sets a metadata value for this object.
        /// </summary>
        /// <param name="key">The metadata key</param>
        /// <param name="value">The metadata value</param>
        void SetMetadata(string key, object value);
        
        /// <summary>
        /// Gets a specific metadata value.
        /// </summary>
        /// <typeparam name="T">The expected type of the metadata value</typeparam>
        /// <param name="key">The metadata key</param>
        /// <param name="defaultValue">The default value to return if the key doesn't exist</param>
        /// <returns>The metadata value if it exists and matches the expected type; otherwise, the default value</returns>
        T GetMetadata<T>(string key, T defaultValue = default(T));
        
        /// <summary>
        /// Checks if a metadata key exists.
        /// </summary>
        /// <param name="key">The metadata key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        bool HasMetadata(string key);
        
        /// <summary>
        /// Removes a metadata key-value pair.
        /// </summary>
        /// <param name="key">The metadata key to remove</param>
        /// <returns>True if the key was removed, false if it didn't exist</returns>
        bool RemoveMetadata(string key);
    }
} 