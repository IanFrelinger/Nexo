using System;
using System.Collections.Generic;

namespace Nexo.Shared.IO
{
    /// <summary>
    /// Base class for all inputs with common properties
    /// </summary>
    public abstract class BaseInput
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CorrelationId { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";

        /// <summary>
        /// Gets a parameter value with type safety
        /// </summary>
        public T? GetParameter<T>(string key, T? defaultValue = default)
        {
            if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// Sets a parameter value
        /// </summary>
        public void SetParameter<T>(string key, T value)
        {
            Parameters[key] = value!;
        }
    }
}
