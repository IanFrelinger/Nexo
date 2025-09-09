using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Infrastructure.Services.Caching.Advanced
{
    /// <summary>
    /// Cache strategy that compresses values to reduce memory usage.
    /// Part of Phase 3.3 advanced caching features.
    /// </summary>
    public class CompressedCacheStrategy<TKey, TValue> : ICacheStrategy<TKey, TValue> where TKey : notnull
    {
        private readonly ICacheStrategy<TKey, byte[]> _innerStrategy;
        private readonly CompressionConfiguration _configuration;

        public CompressedCacheStrategy(ICacheStrategy<TKey, TValue> innerStrategy, CompressionConfiguration? configuration = null)
        {
            _innerStrategy = new CompressedInnerStrategy<TKey, TValue>(innerStrategy);
            _configuration = configuration ?? new CompressionConfiguration();
        }

        /// <summary>
        /// Gets a value from the cache and decompresses it.
        /// </summary>
        public async Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var compressedData = await _innerStrategy.GetAsync(key, cancellationToken);
            if (compressedData == null || compressedData.Length == 0)
            {
                return default(TValue)!;
            }

            return DecompressValue(compressedData);
        }

        /// <summary>
        /// Sets a value in the cache after compressing it.
        /// </summary>
        public async Task SetAsync(TKey key, TValue value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            var compressedData = CompressValue(value);
            await _innerStrategy.SetAsync(key, compressedData, ttl, cancellationToken);
        }

        /// <summary>
        /// Removes a value from the cache.
        /// </summary>
        public async Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            await _innerStrategy.RemoveAsync(key, cancellationToken);
        }

        /// <summary>
        /// Clears all entries from the cache.
        /// </summary>
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await _innerStrategy.ClearAsync(cancellationToken);
        }

        /// <summary>
        /// Invalidates a cache entry.
        /// </summary>
        public async Task InvalidateAsync(TKey key, CancellationToken cancellationToken = default)
        {
            await _innerStrategy.InvalidateAsync(key, cancellationToken);
        }

        /// <summary>
        /// Compresses a value using the configured compression algorithm.
        /// </summary>
        private byte[] CompressValue(TValue value)
        {
            if (value == null)
            {
                return Array.Empty<byte>();
            }

            var json = JsonSerializer.Serialize(value);
            var data = Encoding.UTF8.GetBytes(json);

            // Only compress if the data is large enough to benefit from compression
            if (data.Length < _configuration.MinCompressionSize)
            {
                return data;
            }

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                gzip.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }

        /// <summary>
        /// Decompresses a value using the configured compression algorithm.
        /// </summary>
        private TValue DecompressValue(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
            {
                return default(TValue)!;
            }

            // Check if data is compressed (has GZIP header)
            if (IsCompressed(compressedData))
            {
                using var input = new MemoryStream(compressedData);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                var decompressedData = output.ToArray();
                var json = Encoding.UTF8.GetString(decompressedData);
                return JsonSerializer.Deserialize<TValue>(json) ?? default(TValue)!;
            }
            else
            {
                // Data is not compressed, deserialize directly
                var json = Encoding.UTF8.GetString(compressedData);
                return JsonSerializer.Deserialize<TValue>(json) ?? default(TValue)!;
            }
        }

        /// <summary>
        /// Checks if data is compressed by looking for GZIP header.
        /// </summary>
        private static bool IsCompressed(byte[] data)
        {
            return data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b;
        }

        /// <summary>
        /// Inner strategy that handles the actual caching of compressed data.
        /// </summary>
        private class CompressedInnerStrategy<TKey, TValue> : ICacheStrategy<TKey, byte[]> where TKey : notnull
        {
            private readonly ICacheStrategy<TKey, TValue> _innerStrategy;

            public CompressedInnerStrategy(ICacheStrategy<TKey, TValue> innerStrategy)
            {
                _innerStrategy = innerStrategy;
            }

            public async Task<byte[]> GetAsync(TKey key, CancellationToken cancellationToken = default)
            {
                var value = await _innerStrategy.GetAsync(key, cancellationToken);
                if (value == null)
                {
                    return Array.Empty<byte>();
                }

                var json = JsonSerializer.Serialize(value);
                return Encoding.UTF8.GetBytes(json);
            }

            public async Task SetAsync(TKey key, byte[] value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
            {
                if (value == null || value.Length == 0)
                {
                    await _innerStrategy.SetAsync(key, default(TValue)!, ttl, cancellationToken);
                    return;
                }

                var json = Encoding.UTF8.GetString(value);
                var deserializedValue = JsonSerializer.Deserialize<TValue>(json) ?? default(TValue)!;
                await _innerStrategy.SetAsync(key, deserializedValue, ttl, cancellationToken);
            }

            public async Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
            {
                await _innerStrategy.RemoveAsync(key, cancellationToken);
            }

            public async Task ClearAsync(CancellationToken cancellationToken = default)
            {
                await _innerStrategy.ClearAsync(cancellationToken);
            }

            public async Task InvalidateAsync(TKey key, CancellationToken cancellationToken = default)
            {
                await _innerStrategy.InvalidateAsync(key, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Configuration for compression settings.
    /// </summary>
    public class CompressionConfiguration
    {
        public int MinCompressionSize { get; set; } = 1024; // 1KB
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;
        public bool EnableCompression { get; set; } = true;
    }
}