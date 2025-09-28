using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nexo.Shared.Configuration
{
    /// <summary>
    /// Centralized configuration management system
    /// </summary>
    public static class ConfigurationManager
    {
        private static readonly Dictionary<string, object> _configuration = new();
        private static readonly Dictionary<string, Type> _configurationTypes = new();

        /// <summary>
        /// Registers a configuration type
        /// </summary>
        public static void RegisterConfiguration<T>(string key) where T : class, new()
        {
            _configurationTypes[key] = typeof(T);
        }

        /// <summary>
        /// Gets configuration value with type safety
        /// </summary>
        public static T? GetConfiguration<T>(string key, T? defaultValue = default)
        {
            if (_configuration.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// Sets configuration value
        /// </summary>
        public static void SetConfiguration<T>(string key, T value)
        {
            _configuration[key] = value!;
        }

        /// <summary>
        /// Loads configuration from JSON file
        /// </summary>
        public static async Task LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            
            if (config != null)
            {
                foreach (var (key, value) in config)
                {
                    _configuration[key] = value;
                }
            }
        }

        /// <summary>
        /// Saves configuration to JSON file
        /// </summary>
        public static async Task SaveToFileAsync(string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_configuration, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Gets all configuration keys
        /// </summary>
        public static IEnumerable<string> GetAllKeys()
        {
            return _configuration.Keys;
        }

        /// <summary>
        /// Clears all configuration
        /// </summary>
        public static void Clear()
        {
            _configuration.Clear();
            _configurationTypes.Clear();
        }
    }
}
