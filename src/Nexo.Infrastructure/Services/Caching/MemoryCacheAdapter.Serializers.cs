using System;
using System.Text.Json;
using Nexo.Core.Application.Interfaces.Caching;

namespace Nexo.Infrastructure.Services.Caching
{
    /// <summary>
    /// Cache serializers and eviction policies
    /// </summary>
    public partial class MemoryCacheAdapter
    {
        // Serializers and policies are defined in separate files
    }

    /// <summary>
    /// JSON-based cache serializer.
    /// </summary>
    public class JsonCacheSerializer : ICacheSerializer
    {
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public string Serialize<T>(T value)
        {
            if (value is string str)
            {
                Console.WriteLine($"[JsonCacheSerializer] Serialize<T>: T=string, value='{str}' (return as-is)");
                return str;
            }
            var serialized = JsonSerializer.Serialize(value, _options);
            Console.WriteLine($"[JsonCacheSerializer] Serialize<T>: T={typeof(T)}, value='{value}' => '{serialized}'");
            return serialized;
        }

        public T? Deserialize<T>(string value)
        {
            if (typeof(T) == typeof(string))
            {
                Console.WriteLine($"[JsonCacheSerializer] Deserialize<T>: T=string, value='{value}' (return as-is)");
                return (T)(object)value;
            }
            var deserialized = JsonSerializer.Deserialize<T>(value, _options);
            Console.WriteLine($"[JsonCacheSerializer] Deserialize<T>: T={typeof(T)}, value='{value}' => '{deserialized}'");
            return deserialized;
        }
    }

    /// <summary>
    /// LRU (Least Recently Used) eviction policy.
    /// </summary>
    public class LruEvictionPolicy : ICacheEvictionPolicy
    {
        public IEnumerable<CacheItem> SelectForEviction(IEnumerable<CacheItem> items, int count)
        {
            return items
                .OrderBy(x => x.LastAccessedAt)
                .ThenBy(x => x.Priority)
                .Take(count);
        }
    }
}
