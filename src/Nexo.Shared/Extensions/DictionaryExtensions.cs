using System;
using System.Collections.Generic;

namespace Nexo.Shared.Extensions
{
    /// <summary>
    /// Extension methods for dictionary operations
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Safely gets a value from a dictionary
        /// </summary>
        public static T? GetValueOrDefault<T>(this Dictionary<string, object> dictionary, string key, T? defaultValue = default)
        {
            if (dictionary.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// Safely sets a value in a dictionary
        /// </summary>
        public static void SetValue<T>(this Dictionary<string, object> dictionary, string key, T value)
        {
            dictionary[key] = value!;
        }
    }
}
