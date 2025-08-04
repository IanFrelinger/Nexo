using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Infrastructure.Services.Caching;
using StackExchange.Redis;

namespace Nexo.Infrastructure.Tests.Services.Caching
{
    public class RedisCacheStrategyTests
    {
        [Fact]
        public async Task SetAsync_And_GetAsync_WithTTL_WorksCorrectly()
        {
            // Note: This test requires a Redis instance
            // For CI/CD, you might want to use Testcontainers or skip if Redis is not available
            try
            {
                var redis = ConnectionMultiplexer.Connect("localhost:6379");
                var cache = new RedisCacheStrategy<string, int>(redis);
                
                await cache.SetAsync("test-key", 123, TimeSpan.FromSeconds(1));
                var value1 = await cache.GetAsync("test-key");
                Assert.Equal(123, value1);
                
                await Task.Delay(1100); // Wait for expiration
                var value2 = await cache.GetAsync("test-key");
                Assert.Equal(0, value2); // Should be expired
            }
            catch (RedisConnectionException)
            {
                // Skip test if Redis is not available
                Console.WriteLine("Redis not available, skipping test");
            }
        }

        [Fact]
        public async Task RemoveAsync_And_ClearAsync_WorkCorrectly()
        {
            try
            {
                var redis = ConnectionMultiplexer.Connect("localhost:6379");
                var cache = new RedisCacheStrategy<string, string>(redis);
                
                await cache.SetAsync("key1", "value1");
                await cache.SetAsync("key2", "value2");
                
                var value1 = await cache.GetAsync("key1");
                Assert.Equal("value1", value1);
                
                await cache.RemoveAsync("key1");
                var value2 = await cache.GetAsync("key1");
                Assert.Null(value2);
                
                await cache.ClearAsync();
                var value3 = await cache.GetAsync("key2");
                Assert.Null(value3);
            }
            catch (RedisConnectionException)
            {
                Console.WriteLine("Redis not available, skipping test");
            }
        }
    }
} 