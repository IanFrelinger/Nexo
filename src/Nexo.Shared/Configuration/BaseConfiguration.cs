using System;
using System.Collections.Generic;
using Nexo.Shared.Validation;

namespace Nexo.Shared.Configuration
{
    /// <summary>
    /// Base class for all configuration objects
    /// </summary>
    public abstract class BaseConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public virtual ValidationResult Validate()
        {
            return this.Validate();
        }

        /// <summary>
        /// Gets metadata value with type safety
        /// </summary>
        public T? GetMetadata<T>(string key, T? defaultValue = default)
        {
            if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// Sets metadata value
        /// </summary>
        public void SetMetadata<T>(string key, T value)
        {
            Metadata[key] = value!;
        }
    }
}
