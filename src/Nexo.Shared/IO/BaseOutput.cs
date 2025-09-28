using System;
using System.Collections.Generic;

namespace Nexo.Shared.IO
{
    /// <summary>
    /// Base class for all outputs with common properties
    /// </summary>
    public abstract class BaseOutput
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ExecutionId { get; set; } = string.Empty;
        public object? Data { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public string ExecutedBy { get; set; } = string.Empty;

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
